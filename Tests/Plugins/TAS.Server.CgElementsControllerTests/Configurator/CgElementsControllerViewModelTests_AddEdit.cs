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
    public class CgElementsControllerViewModelTests_AddEdit
    {
        public static IEnumerable<object[]> GetCgElementsController()
        {
            yield return new object[] { null };
            yield return new object[] { new CgElementsController.Configurator.Model.CgElementsController() };
            yield return new object[]
            {
                new CgElementsController.Configurator.Model.CgElementsController()
                {
                    Crawls = new List<ICGElement>
                    {
                        new CgElement { Id = 100 }
                    }
                }
            };
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
    }
}
