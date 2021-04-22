using System;
using System.Collections.ObjectModel;
using TAS.Common.Interfaces;
using TAS.Database.Common;
using TAS.Server.Advantech.Model;

namespace TAS.Server.Advantech.Configurator.Model
{
    public class Gpi : GpiBase, IStartGpi, IPlugin
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();        
        private ObservableCollection<GpiBinding> _bindings = new ObservableCollection<GpiBinding>();
        
        public Gpi()
        {
            GpiChanged += GpiStateChanged;
            _bindings.CollectionChanged += Bindings_CollectionChanged;            
        }

        private void GpiStateChanged(object sender, GpiStateChangedEventArgs e)
        {
            foreach (var binding in Bindings)
                binding.NotifyChange(e.DeviceId, e.Port, e.Pin, e.NewState);
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
                var old = _bindings;

                if (value == _bindings)
                    return;

                _bindings = value;

                if (old != null)
                    old.CollectionChanged -= Bindings_CollectionChanged;

                if (_bindings == null)
                    return;

                old.CollectionChanged += Bindings_CollectionChanged;
                foreach (var item in value)
                {
                    if (_devices.TryGetValue(item.DeviceId, out _))
                        return;

                    _devices.Add(item.DeviceId, new AdvantechDevice(item.DeviceId));
                }
            }
        }

        [Hibernate]
        public bool IsEnabled { get; set; }

        public event EventHandler Started;

        public void Dispose()
        {            
            GpiChanged -= GpiStateChanged;                        
            Logger.Trace("Gpi disposed");
        }        
    }
}
