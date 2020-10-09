﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using TAS.Client.Config.Model;
using TAS.Server.VideoSwitch.Configurator;

namespace TAS.Server.VideoSwitchTests.Configurator
{
    [TestClass]
    public class RouterViewModelTests
    {
        private const string testIp = "127.0.0.1";
        private const int testLevel = 2;
        private const string testLogin = "testLogin";
        private const string testPassword = "testPassword";

        public static IEnumerable<object[]> GetRouter()
        {
            foreach (var router in RouterTestData.Routers)
                yield return new object[] { router };
        }

        private RouterConfiguratorViewModel _routerViewModel = new RouterConfiguratorViewModel(new Engine());

        public void Init(VideoSwitch.VideoSwitch router)
        {
            _routerViewModel.Initialize(router);
            _routerViewModel.IsEnabled = true;
            _routerViewModel.IsModified = false;
        }
       
        [TestMethod]
        [DynamicData(nameof(GetRouter), DynamicDataSourceType.Method)]
        public void EditRouter_Confirm(VideoSwitch.VideoSwitch router)
        {            
            Init(router);

            _routerViewModel.IpAddress = testIp;
            _routerViewModel.Level = testLevel;
            _routerViewModel.Login = testLogin;
            _routerViewModel.Password = testPassword;
            _routerViewModel.SelectedCommunicatorType = VideoSwitch.VideoSwitch.Type.BlackmagicSmartVideoHub;
            _routerViewModel.CommandAddPort.Execute(null);

            _routerViewModel.CommandSave.Execute(null);

            var result = (VideoSwitch.VideoSwitch)_routerViewModel.GetModel();
            Assert.IsNotNull(result, "Object returned from VM is null");

            Assert.AreEqual(result.IpAddress, testIp);
            Assert.AreEqual(result.Level, testLevel);
            Assert.AreEqual(result.Login, testLogin);
            Assert.AreEqual(result.Password, testPassword);
            Assert.AreEqual(result.Type, VideoSwitch.VideoSwitch.Type.BlackmagicSmartVideoHub);
        }
       
        [TestMethod]
        [DynamicData(nameof(GetRouter), DynamicDataSourceType.Method)]
        public void EditRouter_Undo(VideoSwitch.VideoSwitch router)
        {
            Init(router);

            _routerViewModel.IpAddress = testIp;
            _routerViewModel.Level = testLevel;
            _routerViewModel.Login = testLogin;
            _routerViewModel.Password = testPassword;
            _routerViewModel.SelectedCommunicatorType = _routerViewModel.CommunicatorTypes.LastOrDefault();

            _routerViewModel.CommandUndo.Execute(null);            

            Assert.AreEqual(_routerViewModel.IpAddress, router?.IpAddress);
            Assert.AreEqual(_routerViewModel.Level, router?.Level ?? 0);
            Assert.AreEqual(_routerViewModel.Login, router?.Login);
            Assert.AreEqual(_routerViewModel.Password, router?.Password);
            if (router != null)
                Assert.AreEqual(_routerViewModel?.SelectedCommunicatorType, router.Type);
            
        }

        [TestMethod]
        [DynamicData(nameof(GetRouter), DynamicDataSourceType.Method)]
        public void IsEnabled_Changed(VideoSwitch.VideoSwitch router)
        {
            Init(router);
            if (router != null)
                Assert.AreEqual(_routerViewModel.IsEnabled, router.IsEnabled);


            _routerViewModel.IsEnabled = true;
            Assert.IsTrue(_routerViewModel.IsEnabled && ((VideoSwitch.VideoSwitch)_routerViewModel.GetModel()).IsEnabled);
            _routerViewModel.IsEnabled = false;
            Assert.IsFalse(_routerViewModel.IsEnabled || ((VideoSwitch.VideoSwitch)_routerViewModel.GetModel()).IsEnabled);
        }

        [TestMethod]
        [DynamicData(nameof(GetRouter), DynamicDataSourceType.Method)]
        public void IsModified_Modify_True(VideoSwitch.VideoSwitch router)
        {
            Init(router);

            if (!_routerViewModel.IsEnabled)
                return;

            Assert.IsFalse(_routerViewModel.IsModified);
           
            _routerViewModel.IsModified = false;
            _routerViewModel.CommandAddPort.Execute(null);
            Assert.IsTrue(_routerViewModel.IsModified);

            _routerViewModel.IsModified = false;
            _routerViewModel.IpAddress = testIp;
            Assert.IsTrue(_routerViewModel.IsModified);

            _routerViewModel.IsModified = false;
            _routerViewModel.Level = testLevel;
            Assert.IsTrue(_routerViewModel.IsModified);

            _routerViewModel.IsModified = false;
            _routerViewModel.Login = testLogin;
            Assert.IsTrue(_routerViewModel.IsModified);

            _routerViewModel.IsModified = false;
            _routerViewModel.Password = testPassword;
            Assert.IsTrue(_routerViewModel.IsModified);

            _routerViewModel.IsModified = false;
            _routerViewModel.SelectedCommunicatorType = _routerViewModel.CommunicatorTypes.FirstOrDefault(t => t != router?.Type);
            Assert.IsTrue(_routerViewModel.IsModified);
        }
    }
}
