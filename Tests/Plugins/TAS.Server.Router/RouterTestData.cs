using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static TAS.Server.Router.Router;

namespace TAS.Server.RouterTests
{
    [TestClass]
    public class RouterTestData
    {
        public static List<Router.Router> Routers = new List<Router.Router>
        {
            null,
            new Router.Router(RouterType.BlackmagicSmartVideoHub)
            {
                IsEnabled = true,
                IpAddress = "192.168.0.1",
                OutputPorts = new short[] {0,1}
            },
            new Router.Router(RouterType.Nevion)
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
