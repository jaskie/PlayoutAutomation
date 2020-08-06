using Microsoft.VisualStudio.TestTools.UnitTesting;
using TAS.Server.Advantech.Configurator;

namespace TAS.Server.AdvantechTests.Configurator
{
    [TestClass]
    public class GpiBindingViewModelTests
    {
        private static readonly byte _testDeviceId = 1;        
        private static readonly int _testPortNumber = 2;
        private static readonly byte _testPinNumber = 3;

        private GpiBindingViewModel _gpiBindingViewModel = null;
        

        [TestMethod]
        public void AddGpiBinding()
        {            
            _gpiBindingViewModel = new GpiBindingViewModel();

            _gpiBindingViewModel.DeviceId = _testDeviceId;
            _gpiBindingViewModel.PortNumber = _testPortNumber;
            _gpiBindingViewModel.PinNumber = _testPinNumber;            

            _gpiBindingViewModel.Ok(null);

            var result = _gpiBindingViewModel.GpiBinding;
            Assert.AreEqual(_testDeviceId, result.DeviceId);
            Assert.AreEqual(_testPortNumber, result.PortNumber);
            Assert.AreEqual(_testPinNumber, result.PinNumber);            
        }

        [TestMethod]
        public void IsModified_Modify_True()
        {            
            _gpiBindingViewModel = new GpiBindingViewModel();
            Assert.IsFalse(_gpiBindingViewModel.IsModified);

            _gpiBindingViewModel.IsModified = false;
            _gpiBindingViewModel.DeviceId = _testDeviceId;
            Assert.IsTrue(_gpiBindingViewModel.IsModified);

            _gpiBindingViewModel.IsModified = false;
            _gpiBindingViewModel.PortNumber = _testPortNumber;
            Assert.IsTrue(_gpiBindingViewModel.IsModified);

            _gpiBindingViewModel.IsModified = false;
            _gpiBindingViewModel.PinNumber = _testPinNumber;
            Assert.IsTrue(_gpiBindingViewModel.IsModified);            
        }
    }
}
