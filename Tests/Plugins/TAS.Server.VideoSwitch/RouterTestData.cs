using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TAS.Common.Interfaces;

namespace TAS.Server.VideoSwitchTests
{
    [TestClass]
    public class RouterTestData
    {
        public static List<IVideoSwitch> Routers = new List<IVideoSwitch>
        {
            null,
            new VideoSwitch.Model.SmartVideoHub()
            {
                IsEnabled = true,
                IpAddress = "192.168.0.1",
                Outputs = new short[] {0,1}
            },
            new VideoSwitch.Model.Nevion()
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
