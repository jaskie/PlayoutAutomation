using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TAS.Server.VideoSwitchTests
{
    [TestClass]
    public class RouterTestData
    {
        public static List<VideoSwitch.VideoSwitch> Routers = new List<VideoSwitch.VideoSwitch>
        {
            null,
            new VideoSwitch.VideoSwitch(VideoSwitch.VideoSwitch.Type.BlackmagicSmartVideoHub)
            {
                IsEnabled = true,
                IpAddress = "192.168.0.1",
                OutputPorts = new short[] {0,1}
            },
            new VideoSwitch.VideoSwitch(VideoSwitch.VideoSwitch.Type.Nevion)
            {
                IsEnabled = true,
                IpAddress = "192.168.0.1",
                Login = "admin",
                Password = "password",
                OutputPorts = new short[] {0,1}
            }
        };        
    }
}
