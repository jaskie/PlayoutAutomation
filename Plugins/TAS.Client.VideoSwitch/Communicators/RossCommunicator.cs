﻿using System;
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

        public event EventHandler<EventArgs<CrosspointInfo>> SourceChanged;

        private bool _transitionTypeChanged;

        private MessageRequest _takeRequest = new MessageRequest();
        private MessageRequest _crosspointStatusRequest = new MessageRequest();
        private MessageRequest _signalPresenceRequest = new MessageRequest();

        private VideoSwitcherTransitionStyle _videoSwitcherTransitionStyle;

        private PortInfo[] _sources;
        
        public RossCommunicator(): base(9001)
        {

        }

        protected override void OnMessageReceived(byte[] message)
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

                //Response from non functional command. I use it as ping
                case 0x5E:
                    _signalPresenceRequest.SetResult(message);
                    Logger.Trace("Ross ping successful");
                    break;

                case 0x4F:
                    _takeRequest.SetResult(message);
                    break;
            }
        }


        private void ConnectionWatcherProc()
        {
            //PING, non functional command
            const string pingCommand = "FF 1E";
            
            //if (!_semaphores.TryGetValue(ListTypeEnum.SignalPresence, out var semaphore))
            //    return;

            //while (!_shutdownTokenSource.IsCancellationRequested)
            //{
            //        AddToRequestQueue(pingCommand);
            //        if (semaphore.Wait(3000, _shutdownTokenSource.Token))
            //            _shutdownTokenSource.Token.WaitHandle.WaitOne(3000);
            //}
            //Logger.Debug("Connection watcher thread finished.");
        }

        public override void Disconnect()
        {
            base.Disconnect();
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
                    return new CrosspointInfo(DeserializeInputIndex(_crosspointStatusRequest.WaitForResult(DisconnectTokenSource.Token)), -1);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        Logger.Debug("Current Input Port request cancelled");
                    return null;
                }
            }
        }

        private byte[] SerializeInputIndex(int b)
        {
            if (b < 21)
                return new byte[] { (byte)b };

            return new byte[] { 127, (byte)((b >> 7) & 0x7F), (byte)(b & 0x7F) };
        }

        private short DeserializeInputIndex(byte[] message)
        {
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

            //AddToRequestQueue($"FF 09 {SerializeInputIndex(inPort)}");

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
            _videoSwitcherTransitionStyle = videoSwitchEffect;
            _transitionTypeChanged = true;
        }

        public void Preload(int sourceId)
        {
            //while (_takeExecuting)
            //{
            //    Logger.Trace("Waiting Preload");
            //    _waitForTransitionEndSemaphore.Wait();
            //}

            //Logger.Trace("Setting preview {0}", sourceId);
            //AddToRequestQueue($"FF 0B {SerializeInputIndex(sourceId)}");

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

            //Logger.Trace("Executing take");
            //AddToRequestQueue("FF 0F");
        }

        public PortInfo[] Sources
        {
            get => _sources;
            set => SetField(ref _sources, value);
        }
    }
}
