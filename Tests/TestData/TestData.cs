using System.Collections.Generic;
using System.Collections.ObjectModel;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Server.VideoSwitch;


/* example JSON from Mysql (without Plugins)
 * {
	"AspectRatioControl" : 0,	
	"CGStartDelay" : 0,
	"CrawlEnableBehavior" : 0,
	"EnableCGElementsForNewEvents" : false,
	"EngineName" : "ggggg",	
	"StudioMode" : false,
	"TimeCorrection" : 0,
	"VideoFormat" : 0
}
 */

namespace TestData
{
	//Tests get test data from this class. Simply add another objects to collections to test another use cases.
    public class TestEngines
    {
		public static List<IConfigEngine> ConfigEngines = new List<IConfigEngine>
		{
			new TAS.Client.Config.Model.Engine
			{
				AspectRatioControl = TAS.Common.TAspectRatioControl.ImageResize,
				CGStartDelay = 0,
				CrawlEnableBehavior = 0,
				EnableCGElementsForNewEvents = false,
				EngineName = "TestEngine1",
				StudioMode = false,
				TimeCorrection = 0,
				VideoFormat = TAS.Common.TVideoFormat.HD1080i5000
			},
			new TAS.Client.Config.Model.Engine
			{
				AspectRatioControl = TAS.Common.TAspectRatioControl.ImageResize,
				CGElementsController = new TAS.Server.CgElementsController.Configurator.Model.CgElementsController()
				{
					IsEnabled = true,
					Crawls = new List<ICGElement>
					{
						new TAS.Server.CgElementsController.Configurator.Model.CgElement { Id = 0, Name = "Off", Command = "PLAY CG3 EMPTY MIX 25" },
						new TAS.Server.CgElementsController.Configurator.Model.CgElement { Id = 1, Name = "Test1Crawl", Command = "Test1CommandCrawl" },
						new TAS.Server.CgElementsController.Configurator.Model.CgElement { Id = 2, Name = "Test2Crawl", Command = "Test2CommandCrawl" }
					},
					Logos = new List<ICGElement>
					{
						new TAS.Server.CgElementsController.Configurator.Model.CgElement { Id = 0, Name = "Off", Command = "PLAY CG4 EMPTY MIX 25" },
						new TAS.Server.CgElementsController.Configurator.Model.CgElement { Id = 1, Name = "Test1Logo", Command = "Test1CommandLogo" },
						new TAS.Server.CgElementsController.Configurator.Model.CgElement { Id = 2, Name = "Test2Logo", Command = "Test2CommandLogo" }
					},
					Parentals = new List<ICGElement>
					{
						new TAS.Server.CgElementsController.Configurator.Model.CgElement { Id = 0, Name = "Off", Command = "PLAY CG5 EMPTY MIX 25" },
						new TAS.Server.CgElementsController.Configurator.Model.CgElement { Id = 1, Name = "Test1Parental", Command = "Test1CommandParental" },
						new TAS.Server.CgElementsController.Configurator.Model.CgElement { Id = 2, Name = "Test2Parental", Command = "Test2CommandParental" }
					},
					Auxes = new List<ICGElement>
					{
						new TAS.Server.CgElementsController.Configurator.Model.CgElement { Id=0, Name="Off", Command = "Test1CommandAux" }
					},
					StartupsCommands = new List<string>
					{
						"Start1",
						"Start2"
					}
				},

				CGStartDelay = 0,
				CrawlEnableBehavior = 0,
				EnableCGElementsForNewEvents = false,
				EngineName = "TestEngine1",
				StudioMode = false,
				TimeCorrection = 0,
				VideoFormat = TAS.Common.TVideoFormat.HD1080i5000
			},
			new TAS.Client.Config.Model.Engine
			{
				AspectRatioControl = TAS.Common.TAspectRatioControl.ImageResize,
				CGStartDelay = 0,
				CrawlEnableBehavior = 0,
				EnableCGElementsForNewEvents = false,
				EngineName = "TestEngine1",
				StudioMode = false,
				TimeCorrection = 0,
				VideoFormat = TAS.Common.TVideoFormat.HD1080i5000,
				Router = new TAS.Server.VideoSwitch.Model.VideoSwitcher
				{
					IpAddress = "127.0.0.1",
					IsEnabled = true,
					Login = "testLogin",
					Password = "testPassword",
					Level = 1,
					Type = TAS.Server.VideoSwitch.Model.CommunicatorType.BlackmagicSmartVideoHub,
					OutputPorts = new short[2] { 0,1 }
				}
			},
			new TAS.Client.Config.Model.Engine
			{
				AspectRatioControl = TAS.Common.TAspectRatioControl.ImageResize,
				CGStartDelay = 0,
				CrawlEnableBehavior = 0,
				EnableCGElementsForNewEvents = false,
				EngineName = "TestEngine1",
				StudioMode = false,
				TimeCorrection = 0,
				VideoFormat = TAS.Common.TVideoFormat.HD1080i5000,
				Gpis = new List<IGpi>
				{
					new TAS.Server.Advantech.Configurator.Model.Gpi
					{
						IsEnabled = true,
						Bindings = new ObservableCollection<TAS.Server.Advantech.Configurator.Model.GpiBinding>
						{
							new TAS.Server.Advantech.Configurator.Model.GpiBinding(1,1,3)                            
						}
                    }
				}
			}
		};		
	}
}
