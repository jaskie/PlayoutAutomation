using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TAS.Server.VideoSwitchTests
{
    [TestClass]
    public class RouterTestData
    {
        public static List<VideoSwitch.Model.VideoSwitcher> Routers = new List<VideoSwitch.Model.VideoSwitcher>
        {
            null,
            new VideoSwitch.Model.VideoSwitcher(VideoSwitch.Model.CommunicatorType.BlackmagicSmartVideoHub)
            {
                IsEnabled = true,
                IpAddress = "192.168.0.1",
                OutputPorts = new short[] {0,1}
            },
            new VideoSwitch.Model.VideoSwitcher(VideoSwitch.Model.CommunicatorType.Nevion)
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
