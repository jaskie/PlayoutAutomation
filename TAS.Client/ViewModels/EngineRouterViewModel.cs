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
        public IList<IRouterPort> InputPorts { get => Router.InputPorts; }
        
        public IRouterPort SelectedInputPort { get => Router.SelectedInputPort; 
            set 
            {
                if (SelectedInputPort == value)
                    return;

                if (value != null)
                    Router.SelectInput(value.PortID); 
            } 
        }

        public IRouter Router { get; private set; }

        public EngineRouterViewModel(IRouter router)
        {
            Router = router;
            Router.PropertyChanged += _router_PropertyChanged;
        }

        private void _router_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(Router.InputPorts):
                    {
                        NotifyPropertyChanged(nameof(InputPorts));
                        break;
                    }

                case nameof(Router.SelectedInputPort):
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
