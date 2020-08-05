using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace TAS.Server.VideoSwitchTests
{
    [TestClass]
    public class CommunicatorTests
    {
        [TestMethod]
        public void BMDApi64LoadTest()
        {
            try
            {
                string pluginsPath = "../../../../../TVPlay/bin/Debug/";
#if RELEASE
            pluginsPath = "../../../../../TVPlay/bin/Release/";
#endif

                Directory.SetCurrentDirectory(pluginsPath);
                Server.VideoSwitch.VideoSwitch videoSwitch = new Server.VideoSwitch.VideoSwitch(VideoSwitch.VideoSwitch.VideoSwitchType.Atem);
            }
            catch(Exception ex)
            {
                Assert.Fail("Could not create ATEM videoswitch. {0}", ex.Message);
            }
            
        }
    }
}
