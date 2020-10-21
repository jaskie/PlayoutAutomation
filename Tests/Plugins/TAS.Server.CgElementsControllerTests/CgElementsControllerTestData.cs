using System.Collections.Generic;
using TAS.Common.Interfaces;
using TAS.Server.CgElementsController.Configurator.Model;

namespace TAS.Server.CgElementsControllerTests
{
    public class CgElementsControllerTestData
    {
        //Add another objects for more use cases
        public static List<CgElementsController.Configurator.Model.CgElementsController> CgElementsControllers = new List<CgElementsController.Configurator.Model.CgElementsController>
        {
            null,
            new CgElementsController.Configurator.Model.CgElementsController(),
            new CgElementsController.Configurator.Model.CgElementsController()
            {
                IsEnabled = true,
                Crawls = new List<ICGElement>
                {
                    new CgElement { Id = 100 }
                }
            },
            new CgElementsController.Configurator.Model.CgElementsController()
            {
                IsEnabled = false,
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
                    StartupsCommands = new List<string>
                    {
                        "Start1",
                        "Start2"
                    }
                }
        };        
    }
}
