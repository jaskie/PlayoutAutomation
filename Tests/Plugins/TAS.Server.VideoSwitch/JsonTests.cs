using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using TAS.Database.Common;

namespace TAS.Server.VideoSwitchTests
{
    [TestClass]
    public class JsonTests
    {
        #region Fields, properties, Data...        
        private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new HibernationContractResolver(),
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
            Error = (sender, args) =>
            {
                if (args.ErrorContext.Error.GetType() == typeof(JsonSerializationException))
                {
                    Logger.LogMessage("Could not deserialize object {0}: {1}", args.ErrorContext.Member?.ToString(), args.ErrorContext.Error.Message);
                    args.ErrorContext.Handled = true;
                }
            }
        };

        private static IEnumerable<object[]> GetRouter()
        {
            foreach (var router in RouterTestData.Routers)
                yield return new object[] { router };
        }
        #endregion

        [TestMethod]
        [DynamicData(nameof(GetRouter), DynamicDataSourceType.Method)]
        public void SerializeAndDeserialize(VideoSwitch.Model.RouterBase router)
        {
            var json = JsonConvert.SerializeObject(router, _jsonSerializerSettings);
            var deserialized = JsonConvert.DeserializeObject<VideoSwitch.Model.RouterBase>(json, _jsonSerializerSettings);

            if (deserialized == null && router != null)
                Assert.Fail("Failed to deserialize router");

            if (deserialized == null)
                return;

            Assert.AreEqual(router.IsEnabled, deserialized.IsEnabled);
            Assert.AreEqual(router.IpAddress, deserialized.IpAddress);
            Assert.AreEqual(router.Login, deserialized.Login);
            Assert.AreEqual(router.Password, deserialized.Password);

            for (int i=0; i<router.Outputs.Length; ++i)
            {
                if (i >= deserialized.Outputs.Length)
                    Assert.Fail("Count of deserialized OutputPorts does not match original");

                Assert.AreEqual(router.Outputs[i], deserialized.Outputs[i], "Deserialized OutputPorts do not match original");
            }                    
        }
    }
}
