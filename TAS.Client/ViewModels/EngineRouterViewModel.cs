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
        private byte _inputID;
        private ObservableCollection<RouterPort> _inputPorts;
        private RouterPort _selectedInputPort;

        public byte InputID { get => _inputID; set => _inputID = value; }
        public ObservableCollection<RouterPort> InputPorts { get => _inputPorts; set => SetField(ref _inputPorts, value); }
        public RouterPort SelectedInputPort { get => _selectedInputPort; set => SetField(ref _selectedInputPort, value); }

        private IRouter _router;

        public EngineRouterViewModel(IRouter router)
        {
            _router = router;           
            InputPorts = new ObservableCollection<RouterPort>(_router.GetInputPorts().Result);
        }                

        protected override void OnDispose()
        {
            
        }
    }
}
