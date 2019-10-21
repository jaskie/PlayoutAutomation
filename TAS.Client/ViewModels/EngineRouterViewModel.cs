using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EngineRouterViewModel : ViewModelBase
    {               
        public IList<IRouterPort> InputPorts { get => _router.InputPorts; }
        
        public IRouterPort SelectedInputPort { get => _router.SelectedInputPort; 
            set 
            {
                if (SelectedInputPort == value)
                    return;

                if (value != null)
                    _router.SelectInput(value.PortID); 
            } 
        }

        private IRouter _router;

        public EngineRouterViewModel(IRouter router)
        {
            _router = router;
            _router.PropertyChanged += _router_PropertyChanged;
        }

        private void _router_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(_router.InputPorts):
                    {
                        NotifyPropertyChanged(nameof(InputPorts));
                        break;
                    }

                case nameof(_router.SelectedInputPort):
                    {
                        NotifyPropertyChanged(nameof(SelectedInputPort));
                        break;
                    }
                    
            }
        }

        protected override void OnDispose()
        {
            
        }
    }
}
