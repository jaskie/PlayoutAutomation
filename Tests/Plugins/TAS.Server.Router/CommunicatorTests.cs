using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;

namespace TAS.Server.VideoSwitchTests
{    
    [TestClass]
    public class CommunicatorTests
    {
        Server.VideoSwitch.VideoSwitch videoSwitch;

        [TestInitialize]
        public void TestInitialize()
        {
            string pluginsPath = "../../../../../TVPlay/bin/Debug/";
#if RELEASE
            pluginsPath = "../../../../../TVPlay/bin/Release/";
#endif

            Directory.SetCurrentDirectory(pluginsPath);

            videoSwitch = new Server.VideoSwitch.VideoSwitch(VideoSwitch.VideoSwitch.VideoSwitchType.Atem);            
        }
        
        [TestMethod]
        public void AtemInputSwitching()
        {
            int interval = 500;
            try
            {                                
                videoSwitch.Connect();                

                videoSwitch.SelectInput(1);                
                Thread.Sleep(interval);                
                videoSwitch.SelectInput(2);
                Thread.Sleep(interval);
                videoSwitch.SelectInput(3);
                Thread.Sleep(interval);
                videoSwitch.SelectInput(4);
                Thread.Sleep(interval);
                videoSwitch.SelectInput(5);
            }
            catch (Exception ex)
            {
                Assert.Fail("Error trying to switch inputs. {0}", ex.Message);
            }
        }
    }
}
