using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using TAS.Common.Interfaces;
using TAS.Database.Common;

namespace TAS.Server.Advantech
{
    [Export(typeof(IPlugin))]
    public class Gpi : IPlugin
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static Dictionary<int, AdvantechDevice> _devices = new Dictionary<int, AdvantechDevice>();
        private List<GpiBinding> _bindings = new List<GpiBinding>();     
        private static Thread _poolingThread;

        private void Initialize()
        {
            if (_poolingThread == null)
                return;

            _poolingThread = new Thread(_advantechPoolingThreadExecute)
            {
                IsBackground = true,
                Name = "Thread for Advantech devices pooling",
                Priority = ThreadPriority.AboveNormal
            };
            _poolingThread.Start();
        }                  

        [Hibernate]
        public List<GpiBinding> Bindings
        {
            get => _bindings;
            set
            {
                if (value == _bindings)
                    return;

                _bindings = value;
                foreach (var item in value)
                {
                    if (_devices.TryGetValue(((GpiBinding)item).Start.DeviceId, out _))
                        return;

                    _devices.Add(((GpiBinding)item).Start.DeviceId, new AdvantechDevice(((GpiBinding)item).Start.DeviceId));
                }

                Initialize();
            }
        }

        [Hibernate]
        public bool IsEnabled { get; set; }

        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;
            foreach (var deviceEntry in _devices)
                deviceEntry.Value.Dispose();
        }

        private void _advantechPoolingThreadExecute()
        {            
            Logger.Debug("Startting AdvantechPoolingThread thread");
            while (_disposed == default(int))
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
                            for (byte bit = 0; bit < 8; bit++)
                            {
                                if ((changedBits & 0x1) > 0)
                                {
                                    foreach (var binding in Bindings)
                                        binding.NotifyChange((byte)deviceEntry.Key, port, bit, (newPortState & 0x1) > 0);
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
