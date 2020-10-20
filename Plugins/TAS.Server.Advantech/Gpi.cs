using System;
using System.Collections.Generic;
using TAS.Common.Interfaces;
using TAS.Database.Common;
using TAS.Server.Advantech.Model;

namespace TAS.Server.Advantech
{
    public class Gpi : GpiBase, IGpi, IPlugin
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
                
        private List<GpiBinding> _bindings = new List<GpiBinding>();     
                             
        [Hibernate]
        public List<GpiBinding> Bindings
        {
            get => _bindings;
            set
            {
                var old = _bindings;
                if (old != null)
                {
                    foreach (var item in old)
                    {
                        item.Started -= GpiTriggered;
                        item.Dispose();
                    }
                }

                if (value == _bindings)
                    return;                                

                _bindings = value;
                                    
                if (value == null)
                    return;

                foreach (var item in _bindings)
                {
                    item.Started += GpiTriggered;
                    if (_devices.TryGetValue(((GpiBinding)item).Start.DeviceId, out _))
                        return;

                    _devices.Add(((GpiBinding)item).Start.DeviceId, new AdvantechDevice(((GpiBinding)item).Start.DeviceId));
                }                
            }
        }

        public event EventHandler Started;

        private void GpiTriggered(object sender, EventArgs e)
        {
            Started?.Invoke(this, EventArgs.Empty);
        }

        [Hibernate]
        public bool IsEnabled { get; set; }

        public void Dispose()
        {
            if (_bindings != null)
                foreach (var binding in _bindings)
                {
                    binding.Started -= GpiTriggered;
                    binding.Dispose();
                }
            
            Logger.Trace("Gpi disposed");
        }
    }
}
