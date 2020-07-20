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
    public class CgElementsControllerViewModelTests_Delete
    {
        private CgElementsControllerViewModel _cgElementsControllerViewModel = new CgElementsControllerViewModel(new Engine());

        public static IEnumerable<object[]> GetCgElementsController()
        {           
            yield return new object[]
            {
                new CgElementsController.Configurator.Model.CgElementsController()
                {
                    Crawls = new List<ICGElement>
                    {
                        new CgElement { Id = 0, Name = "Off", Command = "PLAY CG3 EMPTY MIX 25" },
                        new CgElement { Id = 1, Name = "Test1Crawl", Command = "Test1CommandCrawl" },
                        new CgElement { Id = 2, Name = "Test2Crawl", Command = "Test2CommandCrawl" }
                    },
                    Logos = new List<ICGElement>
                    {
                        new CgElement { Id = 0, Name = "Off", Command = "PLAY CG4 EMPTY MIX 25" },
                        new CgElement { Id = 1, Name = "Test1Logo", Command = "Test1CommandLogo" },
                        new CgElement { Id = 2, Name = "Test2Logo", Command = "Test2CommandLogo" }
                    },
                    Parentals = new List<ICGElement>
                    {
                        new CgElement { Id = 0, Name = "Off", Command = "PLAY CG5 EMPTY MIX 25" },
                        new CgElement { Id = 1, Name = "Test1Parental", Command = "Test1CommandParental" },
                        new CgElement { Id = 2, Name = "Test2Parental", Command = "Test2CommandParental" }
                    },
                    Auxes = new List<ICGElement>
                    {
                        new CgElement { Id=0, Name="Off", Command = "Test1CommandAux" }
                    },
                    Startup = new List<string>
                    {
                        "Start1",
                        "Start2"
                    }
                }
            };
        }

        public void Init(ICGElementsController cgElementsController)
        {
            _cgElementsControllerViewModel.Initialize(cgElementsController);
            _cgElementsControllerViewModel.IsEnabled = true;
            _cgElementsControllerViewModel.IsModified = false;
        }

        [TestMethod]
        [DynamicData(nameof(GetCgElementsController), DynamicDataSourceType.Method)]
        public void DeleteCgElement_CgElement_Deleted(ICGElementsController cgElementsController)
        {            
            Init(cgElementsController);
            CgElement[] cgElements = null;
            foreach (var type in Enum.GetNames(typeof(CgElement.Type)))
            {                
                _cgElementsControllerViewModel.SelectedElementType = type;
                
                var enumerator = _cgElementsControllerViewModel.CgElements.SourceCollection.GetEnumerator();
                enumerator.MoveNext();
                
                //delete element
                _cgElementsControllerViewModel.DeleteCgElementCommand.Execute(enumerator.Current);

                //check if IDs where recalculated
                cgElements = _cgElementsControllerViewModel.CgElements.Cast<CgElement>().ToArray();
                for (int i=0; i< cgElements.Count(); ++i)                
                    Assert.IsTrue(cgElements[i].Id == i);                
            }

            _cgElementsControllerViewModel.SaveCommand.Execute(null);

            var result = (CgElementsController.Configurator.Model.CgElementsController)_cgElementsControllerViewModel.GetModel();
            Assert.IsTrue(result.Logos.FirstOrDefault(l => ((CgElement)l).Name.Contains("Off")) == null);
            Assert.IsTrue(result.Parentals.FirstOrDefault(l => ((CgElement)l).Name.Contains("Off")) == null);
            Assert.IsTrue(result.Crawls.FirstOrDefault(l => ((CgElement)l).Name.Contains("Off")) == null);
            Assert.IsTrue(result.Auxes.Count() == 0);
        }

        [TestMethod]
        [DynamicData(nameof(GetCgElementsController), DynamicDataSourceType.Method)]
        public void DeleteStartup_Startup_Deleted(ICGElementsController cgElementsController)
        {
            Init(cgElementsController);

            var enumerator = _cgElementsControllerViewModel.Startups.SourceCollection.GetEnumerator();
            enumerator.MoveNext();
            _cgElementsControllerViewModel.DeleteStartupCommand.Execute(enumerator.Current);
            _cgElementsControllerViewModel.SaveCommand.Execute(null);

            var result = (CgElementsController.Configurator.Model.CgElementsController)_cgElementsControllerViewModel.GetModel();
            Assert.IsTrue(result.Startup.Count == 1 && result.Startup.Contains("Start2"));
        }

        [TestMethod]
        [DynamicData(nameof(GetCgElementsController), DynamicDataSourceType.Method)]
        public void IsModified_Modify_True(ICGElementsController cgElementsController)
        {
            Init(cgElementsController);
            foreach (var type in Enum.GetNames(typeof(CgElement.Type)))
            {
                _cgElementsControllerViewModel.SelectedElementType = type;

                _cgElementsControllerViewModel.IsModified = false;
                _cgElementsControllerViewModel.DeleteCgElementCommand.Execute(_cgElementsControllerViewModel.CgElements.Cast<CgElement>().FirstOrDefault());
                Assert.IsTrue(_cgElementsControllerViewModel.IsModified);
            }

            _cgElementsControllerViewModel.IsModified = false;
            var enumerator = _cgElementsControllerViewModel.Startups.SourceCollection.GetEnumerator();
            enumerator.MoveNext();
            _cgElementsControllerViewModel.DeleteStartupCommand.Execute(enumerator.Current);
            Assert.IsTrue(_cgElementsControllerViewModel.IsModified);
        }
    }
}
