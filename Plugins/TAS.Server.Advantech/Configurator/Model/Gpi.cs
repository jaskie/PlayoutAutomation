using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using TAS.Common.Interfaces;
using TAS.Database.Common;

namespace TAS.Server.Advantech.Configurator.Model
{
    public class Gpi : IPlugin
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static Dictionary<int, AdvantechDevice> _devices = new Dictionary<int, AdvantechDevice>();
        private ObservableCollection<GpiBinding> _bindings = new ObservableCollection<GpiBinding>();
        private static Thread _poolingThread;
        public Gpi()
        {
            _bindings.CollectionChanged += Bindings_CollectionChanged;
            Initialize();
        }

        private void Initialize()
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

        private void Bindings_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    if (_devices.TryGetValue(((GpiBinding)item).DeviceId, out _))
                        return;

                    _devices.Add(((GpiBinding)item).DeviceId, new AdvantechDevice(((GpiBinding)item).DeviceId));
                }
            }
        }       

        [Hibernate]
        public ObservableCollection<GpiBinding> Bindings
        {
            get => _bindings;
            set
            {
                if (value == _bindings)
                    return;
                _bindings = value;
                foreach (var item in value)
                {
                    if (_devices.TryGetValue(((GpiBinding)item).DeviceId, out _))
                        return;

                    _devices.Add(((GpiBinding)item).DeviceId, new AdvantechDevice(((GpiBinding)item).DeviceId));
                }
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
