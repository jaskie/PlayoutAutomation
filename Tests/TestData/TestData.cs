using System.Collections.Generic;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;

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
            }
        };		
	}
}
