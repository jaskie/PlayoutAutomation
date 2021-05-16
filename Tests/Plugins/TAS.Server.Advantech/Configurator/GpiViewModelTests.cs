using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using TAS.Server.Advantech.Configurator;
using TAS.Server.Advantech.Configurator.Model;

namespace TAS.Server.AdvantechTests.Configurator
{
    [TestClass]
    public class GpiViewModelTests
    {
        public static IEnumerable<object[]> GetGpi()
        {
            foreach (var gpi in AdvantechTestData.Gpis)
                yield return new object[] { gpi };
        }

        private GpiViewModel _gpiViewModel = new GpiViewModel();

        public void Init(Gpi gpi)
        {
            _gpiViewModel.Initialize(gpi);
            _gpiViewModel.IsEnabled = true;
            _gpiViewModel.IsModified = false;
        }

        [TestMethod]
        [DynamicData(nameof(GetGpi), DynamicDataSourceType.Method)]
        public void GpiUndo(Gpi gpi)
        {
            Init(gpi);

            _gpiViewModel.AddGpiBindingCommand.Execute(null);

            _gpiViewModel.GpiBindingViewModel.DeviceId = 9;
            _gpiViewModel.GpiBindingViewModel.PinNumber = 99;
            _gpiViewModel.GpiBindingViewModel.PortNumber = 999;

            _gpiViewModel.GpiBindingViewModel.Ok(null);
            _gpiViewModel.UndoCommand.Execute(null);

            Assert.IsTrue(_gpiViewModel.GpiBindings.SourceCollection.Cast<object>().Count() == (gpi?.Bindings?.Count ?? 0));
        }

        [TestMethod]
        [DynamicData(nameof(GetGpi), DynamicDataSourceType.Method)]
        public void AddGpi(Gpi gpi)
        {
            Init(gpi);
            var initialBindingsCount = gpi?.Bindings?.Count ?? 0;

            _gpiViewModel.AddGpiBindingCommand.Execute(null);

            _gpiViewModel.GpiBindingViewModel.DeviceId = 9;
            _gpiViewModel.GpiBindingViewModel.PinNumber = 99;
            _gpiViewModel.GpiBindingViewModel.PortNumber = 999;

            _gpiViewModel.GpiBindingViewModel.CommandOk.Execute(null);
            _gpiViewModel.SaveCommand.Execute(null);

            var result = (Gpi)_gpiViewModel.GetModel();
            Assert.IsNotNull(result, "Object returned from VM is null");

            Assert.IsTrue(result.Bindings.Count > initialBindingsCount);
        }

        [TestMethod]
        [DynamicData(nameof(GetGpi), DynamicDataSourceType.Method)]
        public void DeleteGpi(Gpi gpi)
        {
            Init(gpi);
            var initialBindingsCount = gpi?.Bindings?.Count;

            var enumerator = _gpiViewModel.GpiBindings.SourceCollection.GetEnumerator();
            if (!enumerator.MoveNext())
                return;
            _gpiViewModel.DeleteGpiBindingCommand.Execute(enumerator.Current);
            _gpiViewModel.SaveCommand.Execute(null);

            var result = (Gpi)_gpiViewModel.GetModel();
            Assert.IsNotNull(result, "Object returned from VM is null");

            Assert.IsTrue(result.Bindings.Count < initialBindingsCount);
        }

        [TestMethod]
        [DynamicData(nameof(GetGpi), DynamicDataSourceType.Method)]
        public void IsEnabled_Changed(Gpi gpi)
        {
            Init(gpi);
            if (gpi != null)
                Assert.AreEqual(_gpiViewModel.IsEnabled, gpi.IsEnabled);

            _gpiViewModel.IsEnabled = true;
            Assert.IsTrue(_gpiViewModel.IsEnabled && ((Gpi)_gpiViewModel.GetModel()).IsEnabled);
            _gpiViewModel.IsEnabled = false;
            Assert.IsFalse(_gpiViewModel.IsEnabled || ((Gpi)_gpiViewModel.GetModel()).IsEnabled);
        }

        [TestMethod]
        [DynamicData(nameof(GetGpi), DynamicDataSourceType.Method)]
        public void IsModified_Modify_True(Gpi gpi)
        {
            Init(gpi);

            Assert.IsFalse(_gpiViewModel.IsModified);

            _gpiViewModel.IsModified = false;
            _gpiViewModel.AddGpiBindingCommand.Execute(null);
            _gpiViewModel.GpiBindingViewModel.IsModified = true;
            _gpiViewModel.GpiBindingViewModel.CommandOk.Execute(null);
            Assert.IsTrue(_gpiViewModel.IsModified);

            _gpiViewModel.IsModified = false;
            var enumerator = _gpiViewModel.GpiBindings.GetEnumerator();
            enumerator.MoveNext();
            _gpiViewModel.DeleteGpiBindingCommand.Execute(enumerator.Current);            
            Assert.IsTrue(_gpiViewModel.IsModified);            
        }
    }
}
