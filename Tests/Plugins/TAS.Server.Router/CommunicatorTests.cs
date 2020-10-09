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

            videoSwitch = new Server.VideoSwitch.VideoSwitch(VideoSwitch.VideoSwitch.Type.Atem);            
        }
        
        [TestMethod]
        public void AtemInputSwitching()
        {
            int interval = 500;
            try
            {                                
                videoSwitch.ConnectAsync();                

                videoSwitch.SetSource(1);                
                Thread.Sleep(interval);                
                videoSwitch.SetSource(2);
                Thread.Sleep(interval);
                videoSwitch.SetSource(3);
                Thread.Sleep(interval);
                videoSwitch.SetSource(4);
                Thread.Sleep(interval);
                videoSwitch.SetSource(5);
            }
            catch (Exception ex)
            {
                Assert.Fail("Error trying to switch inputs. {0}", ex.Message);
            }
        }
    }
}
