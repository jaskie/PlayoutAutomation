using TAS.Client.Common;

namespace TAS.Server.Advantech.Configurator
{
    public class GpiBindingViewModel : OkCancelViewModelBase
    {
        private Model.GpiBinding _gpiBinding;
        private byte _deviceId;
        private int _portNumber;
        private byte _pinNumber;

        public GpiBindingViewModel() : base("Add") { }       

        public override bool Ok(object obj)
        {
            _gpiBinding = new Model.GpiBinding(_deviceId, _portNumber, _pinNumber);            
            return true;
        }
        public Model.GpiBinding GpiBinding => _gpiBinding;
        public byte DeviceId { get => _deviceId; set => SetField(ref _deviceId, value); }
        public int PortNumber { get => _portNumber; set => SetField(ref _portNumber, value); }
        public byte PinNumber { get => _pinNumber; set => SetField(ref _pinNumber, value); }
    }
}
