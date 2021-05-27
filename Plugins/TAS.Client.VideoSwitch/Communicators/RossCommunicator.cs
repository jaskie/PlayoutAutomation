using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Server.VideoSwitch.Helpers;
using TAS.Server.VideoSwitch.Model;
using TAS.Server.VideoSwitch.Model.Interfaces;

namespace TAS.Server.VideoSwitch.Communicators
{   
    /// <summary>
    /// Class to communicate with Ross MC-1 MCR switcher using Pressmaster protocol (default on port 9001)
    /// </summary>
    public class RossCommunicator : SocketConnection, IVideoSwitchCommunicator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        static readonly byte[] PingCommand = { 0xFF, 0x1E };
        
        public event EventHandler<EventArgs<CrosspointInfo>> SourceChanged;

        private readonly MessageRequest _takeRequest = new MessageRequest();
        private readonly MessageRequest _crosspointStatusRequest = new MessageRequest();
        private readonly MessageRequest _signalPresenceRequest = new MessageRequest();
        private Thread _connectionWatcherThread;
        private PortInfo[] _sources;
        
        public RossCommunicator(): base(9001)
        {

        }

        protected override void OnMessageReceived(byte[] messages)
        {
            foreach (var message in Split(messages))
            {
                Logger.Trace("Message received: {0}", BitConverter.ToString(message));
                if (message.Length < 2 || (message[0] != 0xFF && message[0] != 0xFE))
                    return;

                switch (message[1])
                {
                    //Program has been set
                    case 0x49:
                        _crosspointStatusRequest?.SetResult(message);
                        SourceChanged?.Invoke(this, new EventArgs<CrosspointInfo>(new CrosspointInfo(DeserializeInputIndex(message), -1)));
                        break;

                    //Response from ping command.
                    case 0x5E:
                        _signalPresenceRequest?.SetResult(message);
                        break;
                    
                    // Take completed
                    case 0x4F: 
                        _takeRequest?.SetResult(message);
                        break;
                }
            }
        }

        private IEnumerable<byte[]> Split(byte[] messages)
        {
            int start = 0, end;
            while (true)
            {
                var endSimple = Array.IndexOf<byte>(messages, 0xFF, start+1);
                var endExtended = Array.IndexOf<byte>(messages, 0xFE, start+1);
                if ((endSimple > 0) && (endExtended > 0))
                    end = Math.Min(endSimple, endExtended);
                else if (endSimple >= 0)
                    end = endSimple;
                else if (endExtended >= 0)
                    end = endExtended;
                else break;
                var result = new byte[end - start];
                Buffer.BlockCopy(messages, start, result, 0, end - start);
                yield return result;
                start = end;
            }
            end = messages.Length;
            if ((end - start) > 0)
            {
                var result = new byte[end - start];
                Buffer.BlockCopy(messages, start, result, 0, end - start);
                yield return result;
            }
        }


        private async void ConnectionWatcherProc()
        {
            while (true)
            {
                var tokenSource = DisconnectTokenSource;
                if (tokenSource is null)
                    break;
                Send(PingCommand);
                try
                {
                    _signalPresenceRequest.WaitForResult(tokenSource.Token);
#if DEBUG
                    await Task.Delay(30000, tokenSource.Token);
#else
                    await Task.Delay(5000,  tokenSource.Token); 
#endif
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            Logger.Debug("Connection watcher thread finished.");
        }

        public override void Disconnect()
        {
            base.Disconnect();
            _connectionWatcherThread?.Join();
        }

        public override void Dispose()
        {
            base.Dispose();
            _crosspointStatusRequest.Dispose();
            _signalPresenceRequest.Dispose();
            _takeRequest.Dispose();
            Logger.Debug("Ross communicator disposed");
        }

        public CrosspointInfo GetSelectedSource()
        {
            lock (_crosspointStatusRequest.SyncRoot)
            {
                Send(new byte[] { 0xFF, 0x02 });
                try
                {
                    var tokenSource = DisconnectTokenSource;
                    return tokenSource is null ? 
                        throw new OperationCanceledException() :
                        new CrosspointInfo(DeserializeInputIndex(_crosspointStatusRequest.WaitForResult(tokenSource.Token)), -1);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        Logger.Debug("Current Input Port request cancelled");
                    return null;
                }
            }
        }

        private byte[] SerializeInputIndexMessage(byte command, int b)
        {
            if (b < 21)
                return new byte[] { 0xFF, command, (byte)b };

            return new byte[] { 0xFF, command, 127, (byte)((b >> 7) & 0x7F), (byte)(b & 0x7F) };
        }

        private short DeserializeInputIndex(byte[] message)
        {
            if (message is null)
                return -1;
            if (message.Length == 3 && message[2] != 0x7F)
                return message[2];
            //Tally extended message formula
            else if (message.Length == 5 && message[2] == 0x7F)
                return (short)((message[4] & 0x7F) | ((message[3] & 0x7F) << 7));
            Logger.Error("Invalid input: {0}", BitConverter.ToString(message));
            return -1;
        }

        public void SetSource(int inPort)
        {
            //while (_takeExecuting)
            //{
            //    Logger.Trace("Waiting Program");
            //    _waitForTransitionEndSemaphore.Wait(1000, _shutdownTokenSource.Token);
            //}

            Logger.Debug("Setting PGM source to {0}", inPort);
            Send(SerializeInputIndexMessage(0x09 /*set crosspoint on PGM bus*/, inPort));

            //if (_waitForTransitionEndSemaphore.CurrentCount == 0)
            //    _waitForTransitionEndSemaphore.Release();

            //if (!_transitionTypeChanged)
            //    return;
            //SetTransitionStyle(_videoSwitcherTransitionStyle);
            //_transitionTypeChanged = false;
        }

        public override bool Connect(string address)
        {
            var connected = base.Connect(address);
            if (!connected)
                return false;
            _connectionWatcherThread = new Thread(ConnectionWatcherProc)
            {
                Name = $"Ross connection watcher for {address}",
                IsBackground = true
            };
            _connectionWatcherThread.Start();
            return connected;
        }

        public void SetTransitionStyle(VideoSwitcherTransitionStyle videoSwitchEffect)
        {
            switch(videoSwitchEffect)
            {
                case VideoSwitcherTransitionStyle.VFade:
                    Send(new byte[] { 0xFF, 0x01, 0x01 });
                    break;
                case VideoSwitcherTransitionStyle.FadeAndTake:
                    Send(new byte[] { 0xFF, 0x01, 0x02 });
                    break;
                case VideoSwitcherTransitionStyle.Mix:
                    Send(new byte[] { 0xFF, 0x01, 0x03 });
                    break;
                case VideoSwitcherTransitionStyle.TakeAndFade:
                    Send(new byte[] { 0xFF, 0x01, 0x04 });
                    break;
                case VideoSwitcherTransitionStyle.Cut:
                    Send(new byte[] { 0xFF, 0x01, 0x05 });
                    break;
                case VideoSwitcherTransitionStyle.WipeLeft:
                    Send(new byte[] { 0xFF, 0x01, 0x06 });
                    break;
                case VideoSwitcherTransitionStyle.WipeTop:
                    Send(new byte[] { 0xFF, 0x01, 0x07 });
                    break;
                case VideoSwitcherTransitionStyle.WipeReverseLeft:
                    Send(new byte[] { 0xFF, 0x01, 0x10 });
                    break;
                case VideoSwitcherTransitionStyle.WipeReverseTop:
                    Send(new byte[] { 0xFF, 0x01, 0x11 });
                    break;

                default:
                    return;
            }
        }

        public void Preload(int sourceId)
        {
            //while (_takeExecuting)
            //{
            //    Logger.Trace("Waiting Preload");
            //    _waitForTransitionEndSemaphore.Wait();
            //}

            Logger.Debug("Setting preview {0}", sourceId);
            Send(SerializeInputIndexMessage(0x0B /*set crosspoint on PST bus*/, sourceId));

            //if (_waitForTransitionEndSemaphore.CurrentCount == 0)
            //    _waitForTransitionEndSemaphore.Release();
        }
       
        public void SetMixSpeed(byte rate)
        {
            Send(new byte[]{ 0xFF, 0x03, rate});
        }

        public void Take()
        {
            //lock (_syncObject)            
            //    _takeExecuting = true;

            Logger.Debug("Executing take");
            Send(new byte[] { 0xFF, 0x0F });
        }

        public PortInfo[] Sources
        {
            get => _sources;
            set => SetField(ref _sources, value);
        }
    }
}
