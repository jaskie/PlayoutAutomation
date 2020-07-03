using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using TAS.Client.Config.Model;
using TAS.Server.CgElementsController.Configurator;
using TAS.Server.CgElementsController.Configurator.Model;

namespace TAS.Server.CgElementsControllerTests.Configurator
{
    [TestClass]
    public class CgElementsControllerViewModelTests
    {
        private CgElementsControllerViewModel _cgElementsControllerViewModel = new CgElementsControllerViewModel(new Engine());

        [TestInitialize]
        public void Init()
        {
            //to do: call init in cgVm
            //_cgElementsControllerViewModel.Initialize(new CgElementsController());
        }

        [TestMethod]
        public void IsModified_Modify_True()
        {
            CgElement cgElement = null;
            Assert.IsFalse(_cgElementsControllerViewModel.IsModified);

            foreach (var type in Enum.GetValues(typeof(CgElement.Type)))
            {
                cgElement = new CgElement();

                _cgElementsControllerViewModel.SelectedElementType = Enum.GetName(typeof(CgElement.Type), type);
                Assert.IsFalse(_cgElementsControllerViewModel.IsModified);

                _cgElementsControllerViewModel.AddCgElementCommand.Execute(cgElement);
                Assert.IsFalse(_cgElementsControllerViewModel.IsModified);
                Assert.IsTrue(_cgElementsControllerViewModel.CgElements.SourceCollection.Cast<object>().Count() == 1);

                _cgElementsControllerViewModel.IsModified = false;
                _cgElementsControllerViewModel.EditCgElementCommand.Execute(cgElement);
                Assert.IsTrue(_cgElementsControllerViewModel.CgElements.SourceCollection.Cast<object>().Count() == 1);
                Assert.IsFalse(_cgElementsControllerViewModel.IsModified);

                _cgElementsControllerViewModel.IsModified = false;
                _cgElementsControllerViewModel.DeleteCgElementCommand.Execute(cgElement);
                Assert.IsTrue(_cgElementsControllerViewModel.CgElements.SourceCollection.Cast<object>().Count() == 0);
                Assert.IsTrue(_cgElementsControllerViewModel.IsModified);                
            }           

            _cgElementsControllerViewModel.IsModified = false;
            _cgElementsControllerViewModel.AddStartupCommand.Execute(cgElement);
            Assert.IsTrue(_cgElementsControllerViewModel.Startups.SourceCollection.Cast<object>().Count() == 1);
            Assert.IsTrue(_cgElementsControllerViewModel.IsModified);

            _cgElementsControllerViewModel.IsModified = false;
            _cgElementsControllerViewModel.DeleteStartupCommand.Execute(cgElement);
            Assert.IsTrue(_cgElementsControllerViewModel.Startups.SourceCollection.Cast<object>().Count() == 0);
            Assert.IsTrue(_cgElementsControllerViewModel.IsModified);

            _cgElementsControllerViewModel.IsModified = false;
            _cgElementsControllerViewModel.SelectedDefaultCrawl = cgElement;
            Assert.IsTrue(_cgElementsControllerViewModel.IsModified);

            _cgElementsControllerViewModel.IsModified = false;
            _cgElementsControllerViewModel.SelectedDefaultLogo = cgElement;
            Assert.IsTrue(_cgElementsControllerViewModel.IsModified);
        }
    }
}
