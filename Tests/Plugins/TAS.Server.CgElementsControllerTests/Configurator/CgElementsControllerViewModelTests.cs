using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TAS.Client.Config.Model;
using TAS.Common.Interfaces;
using TAS.Server.CgElementsController.Configurator;
using TAS.Server.CgElementsController.Configurator.Model;

namespace TAS.Server.CgElementsControllerTests.Configurator
{
    [TestClass]
    public class CgElementsControllerViewModelTests
    {
        public static IEnumerable<object[]> GetCgElementsController()
        {
            foreach (var cgElementsController in CgElementsControllerTestData.CgElementsControllers)
                yield return new object[] { cgElementsController };            
        }

        private CgElementsControllerViewModel _cgElementsControllerViewModel = new CgElementsControllerViewModel(new Engine());
        
        public void Init(ICGElementsController cgElementsController)
        {            
            _cgElementsControllerViewModel.Initialize(cgElementsController);
            _cgElementsControllerViewModel.IsEnabled = true;
            _cgElementsControllerViewModel.IsModified = false;
        }

        /// <summary>
        /// Tests adding Cg Elements. Tests elements are generated based on type - name of type is inserted to "Command" property, then after VM save, 
        /// test checks if such items exist.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetCgElementsController), DynamicDataSourceType.Method)]
        public void AddCgElement_CgElement_Confirm(ICGElementsController cgElementsController)
        {
            Init(cgElementsController);
                        
            foreach(var type in Enum.GetNames(typeof(CgElement.Type)))
            {                
                _cgElementsControllerViewModel.SelectedElementType = type;
                _cgElementsControllerViewModel.AddCgElementCommand.Execute(null);
                ((CgElementViewModel)_cgElementsControllerViewModel.CgElementViewModel).Command = type;                
                _cgElementsControllerViewModel.CgElementViewModel.CommandOk.Execute(null);
            }            
            _cgElementsControllerViewModel.SaveCommand.Execute(null);

            var returnedObject = (CgElementsController.Configurator.Model.CgElementsController)_cgElementsControllerViewModel.GetModel();
            Assert.IsNotNull(returnedObject, "Object returned from VM is null");
                       
            Assert.IsNotNull(returnedObject.Crawls.FirstOrDefault(a =>
            {
                if (Enum.TryParse<CgElement.Type>(((CgElement)a).Command, out var type))
                    return true;
                return false;
            }), "Inserted values could not be found");
            Assert.IsNotNull(returnedObject.Parentals.FirstOrDefault(a =>
            {
                if (Enum.TryParse<CgElement.Type>(((CgElement)a).Command, out var type))
                    return true;
                return false;
            }), "Inserted values could not be found");
            Assert.IsNotNull(returnedObject.Logos.FirstOrDefault(a =>
            {
                if (Enum.TryParse<CgElement.Type>(((CgElement)a).Command, out var type))
                    return true;
                return false;
            }), "Inserted values could not be found");
        }

        /// <summary>
        /// Tests CgElement edit process. Test will load element to wizard, save it and read new data.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetCgElementsController), DynamicDataSourceType.Method)]
        public void EditCgElement_CgElement_Confirm(ICGElementsController cgElementsController)
        {
            Init(cgElementsController);

            var testString = "CommandEdit";
            CgElement cgElement = new CgElement();

            _cgElementsControllerViewModel.AddCgElementCommand.Execute(cgElement);
            _cgElementsControllerViewModel.EditCgElementCommand.Execute(cgElement);
            Assert.IsTrue(_cgElementsControllerViewModel.CgElementViewModel != null, "VM has not created CgElement wizard");
            ((CgElementViewModel)_cgElementsControllerViewModel.CgElementViewModel).Command = testString;
            _cgElementsControllerViewModel.CgElementViewModel.CommandOk.Execute(null);
            Assert.IsTrue(cgElement.Command == testString, "Item has not been edited");
            Assert.IsTrue(_cgElementsControllerViewModel.CgElementViewModel == null, "Wizard has not disposed");
        }

        [TestMethod]
        [DynamicData(nameof(GetCgElementsController), DynamicDataSourceType.Method)]
        public void IsEnabled_Changed(ICGElementsController cgElementsController)
        {
            Init(cgElementsController);
            if (cgElementsController != null)
                Assert.AreEqual(_cgElementsControllerViewModel.IsEnabled, cgElementsController.IsEnabled);

            _cgElementsControllerViewModel.IsEnabled = true;            
            Assert.IsTrue(((ICGElementsController)_cgElementsControllerViewModel.GetModel()).IsEnabled && _cgElementsControllerViewModel.IsEnabled);
            _cgElementsControllerViewModel.IsEnabled = false;
            Assert.IsFalse(((ICGElementsController)_cgElementsControllerViewModel.GetModel()).IsEnabled || _cgElementsControllerViewModel.IsEnabled);
        }

        /// <summary>
        /// Tests CgElement edit process. Test will load element to wizard, cancel it and read data.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetCgElementsController), DynamicDataSourceType.Method)]
        public void EditCgElement_CgElement_Cancel(ICGElementsController cgElementsController)
        {
            Init(cgElementsController);

            CgElement cgElement = new CgElement { Command = String.Empty };

            _cgElementsControllerViewModel.AddCgElementCommand.Execute(cgElement);
            _cgElementsControllerViewModel.EditCgElementCommand.Execute(cgElement);
            Assert.IsTrue(_cgElementsControllerViewModel.CgElementViewModel != null, "VM has not created CgElement wizard");
            ((CgElementViewModel)_cgElementsControllerViewModel.CgElementViewModel).Command = "DummyData";
            _cgElementsControllerViewModel.CgElementViewModel.CommandCancel.Execute(null);
            Assert.IsTrue(cgElement.Command == String.Empty, "Item has been edited");
            Assert.IsTrue(_cgElementsControllerViewModel.CgElementViewModel == null, "Wizard has not disposed");
        }
       
        [TestMethod]
        [DynamicData(nameof(GetCgElementsController), DynamicDataSourceType.Method)]
        public void IsModified_Modify_True(ICGElementsController cgElementsController)
        {
            Init(cgElementsController);

            CgElement cgElement = null;
            Assert.IsFalse(_cgElementsControllerViewModel.IsModified);

            foreach (var type in Enum.GetValues(typeof(CgElement.Type)))
            {
                cgElement = new CgElement();

                _cgElementsControllerViewModel.SelectedElementType = Enum.GetName(typeof(CgElement.Type), type);
                Assert.IsFalse(_cgElementsControllerViewModel.IsModified);

                _cgElementsControllerViewModel.AddCgElementCommand.Execute(cgElement);
                Assert.IsFalse(_cgElementsControllerViewModel.IsModified);                

                _cgElementsControllerViewModel.IsModified = false;
                _cgElementsControllerViewModel.EditCgElementCommand.Execute(cgElement);                
                Assert.IsFalse(_cgElementsControllerViewModel.IsModified);                        
            }           

            _cgElementsControllerViewModel.IsModified = false;
            _cgElementsControllerViewModel.AddStartupCommand.Execute(cgElement);            
            Assert.IsTrue(_cgElementsControllerViewModel.IsModified);
          
            _cgElementsControllerViewModel.IsModified = false;
            _cgElementsControllerViewModel.SelectedDefaultCrawl = cgElement;
            Assert.IsTrue(_cgElementsControllerViewModel.IsModified);

            _cgElementsControllerViewModel.IsModified = false;
            _cgElementsControllerViewModel.SelectedDefaultLogo = cgElement;
            Assert.IsTrue(_cgElementsControllerViewModel.IsModified);
        }

        [TestMethod]
        [DynamicData(nameof(GetCgElementsController), DynamicDataSourceType.Method)]
        public void DeleteCgElement_CgElement_Deleted(CgElementsController.Configurator.Model.CgElementsController cgElementsController)
        {
            Init(cgElementsController);
            var initialCrawlsCount = cgElementsController?.Crawls?.Count();
            var initialParentalsCount = cgElementsController?.Parentals?.Count();
            var initialLogosCount = cgElementsController?.Logos?.Count();
            var initialAuxesCount = cgElementsController?.Auxes?.Count();

            CgElement[] cgElements = null;
            foreach (var type in Enum.GetNames(typeof(CgElement.Type)))
            {
                _cgElementsControllerViewModel.SelectedElementType = type;

                var enumerator = _cgElementsControllerViewModel.CgElements.SourceCollection.GetEnumerator();
                if (!enumerator.MoveNext())
                    return;

                //delete element
                _cgElementsControllerViewModel.DeleteCgElementCommand.Execute(enumerator.Current);

                //check if IDs where recalculated
                cgElements = _cgElementsControllerViewModel.CgElements.Cast<CgElement>().ToArray();
                for (int i = 0; i < cgElements.Count(); ++i)
                    Assert.IsTrue(cgElements[i].Id == i);
            }

            _cgElementsControllerViewModel.SaveCommand.Execute(null);

            var result = (CgElementsController.Configurator.Model.CgElementsController)_cgElementsControllerViewModel.GetModel();

            if (initialLogosCount > 0)
                Assert.IsTrue(result.Logos.Count() < initialLogosCount);

            if (initialParentalsCount > 0)
                Assert.IsTrue(result.Parentals.Count() < initialParentalsCount);

            if (initialCrawlsCount > 0)
                Assert.IsTrue(result.Crawls.Count() < initialCrawlsCount);

            if (initialAuxesCount > 0)
                Assert.IsTrue(result.Auxes.Count() < initialAuxesCount);
        }

        [TestMethod]
        [DynamicData(nameof(GetCgElementsController), DynamicDataSourceType.Method)]
        public void DeleteStartup_Startup_Deleted(CgElementsController.Configurator.Model.CgElementsController cgElementsController)
        {
            Init(cgElementsController);
            var initialStartupsCount = cgElementsController?.StartupsCommands?.Count;

            var enumerator = _cgElementsControllerViewModel.Startups.SourceCollection.GetEnumerator();
            if (!enumerator.MoveNext())
                return;
            _cgElementsControllerViewModel.DeleteStartupCommand.Execute(enumerator.Current);
            _cgElementsControllerViewModel.SaveCommand.Execute(null);

            var result = (CgElementsController.Configurator.Model.CgElementsController)_cgElementsControllerViewModel.GetModel();
            Assert.IsTrue(result.StartupsCommands.Count < initialStartupsCount);
        }

        [TestMethod]
        [DynamicData(nameof(GetCgElementsController), DynamicDataSourceType.Method)]
        public void IsModified_Modify_True(CgElementsController.Configurator.Model.CgElementsController cgElementsController)
        {
            Init(cgElementsController);
            foreach (var type in Enum.GetNames(typeof(CgElement.Type)))
            {
                _cgElementsControllerViewModel.SelectedElementType = type;
                _cgElementsControllerViewModel.IsModified = false;

                var typeEnumerator = _cgElementsControllerViewModel.CgElements.GetEnumerator();
                if (!typeEnumerator.MoveNext())
                    continue;
                _cgElementsControllerViewModel.DeleteCgElementCommand.Execute(typeEnumerator.Current);
                Assert.IsTrue(_cgElementsControllerViewModel.IsModified);
            }

            _cgElementsControllerViewModel.IsModified = false;
            var enumerator = _cgElementsControllerViewModel.Startups.SourceCollection.GetEnumerator();
            if (!enumerator.MoveNext())
                return;
            _cgElementsControllerViewModel.DeleteStartupCommand.Execute(enumerator.Current);
            Assert.IsTrue(_cgElementsControllerViewModel.IsModified);
        }
    }
}
