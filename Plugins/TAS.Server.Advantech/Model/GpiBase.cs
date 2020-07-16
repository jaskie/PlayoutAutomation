using System;
using System.Collections.Generic;
using System.Threading;
using TAS.Server.Advantech.Model.Args;

namespace TAS.Server.Advantech.Model
{
    public class GpiBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();        
        private static Thread _poolingThread;

        protected static EventHandler<GpiStateChangedEventArgs> GpiChanged;
        protected static Dictionary<int, AdvantechDevice> _devices = new Dictionary<int, AdvantechDevice>();

        public GpiBase()
        {
            if (_poolingThread != null)
                return;

            _poolingThread = new Thread(_advantechPoolingThreadExecute)
            {
                IsBackground = true,
                Name = "Thread for Advantech devices pooling",
                Priority = ThreadPriority.AboveNormal
            };
            _poolingThread.Start();
        }                

        private void _advantechPoolingThreadExecute()
        {
            Logger.Debug("Startting AdvantechPoolingThread thread");
            while (true)
            {
                try
                {
                    foreach (var deviceEntry in _devices)
                    {
                        for (byte port = 0; port < deviceEntry.Value.InputPortCount; port++)
                        {
                            if (!deviceEntry.Value.Read(port, out var newPortState, out var oldPortState))
                                continue;
                            var changedBits = newPortState ^ oldPortState;
                            for (byte pin = 0; pin < 8; pin++)
                            {
                                if ((changedBits & 0x1) > 0)
                                {
                                    GpiChanged?.Invoke(this, new GpiStateChangedEventArgs((byte)deviceEntry.Key, port, pin, (newPortState & 0x1) > 0));
                                }
                                changedBits = changedBits >> 1;
                                newPortState = (byte)(newPortState >> 1);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Warn(e, $"Exception on AdvantechPoolingThread:\n{e}");
                }
                Thread.Sleep(5);
            }            
        }       
    }
}
