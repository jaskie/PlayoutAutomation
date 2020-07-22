using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using TAS.Database.Common;
using TAS.Server.Advantech.Configurator.Model;

namespace TAS.Server.AdvantechTests
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

        private static IEnumerable<object[]> GetGpi()
        {
            foreach (var gpi in AdvantechTestData.Gpis)
                yield return new object[] { gpi };
        }
        #endregion

        [TestMethod]
        [DynamicData(nameof(GetGpi), DynamicDataSourceType.Method)]
        public void SerializeAndDeserialize(Gpi gpi)
        {
            var json = JsonConvert.SerializeObject(gpi, _jsonSerializerSettings);
            var deserialized = JsonConvert.DeserializeObject<Gpi>(json, _jsonSerializerSettings);

            if (deserialized == null && gpi != null)
                Assert.Fail("Failed to deserialize CgElementsController");

            if (deserialized == null)
                return;

            Assert.AreEqual(gpi.IsEnabled, deserialized.IsEnabled, "Gpi deserialized not properly");

            Assert.IsTrue(gpi.Bindings.Count == deserialized.Bindings.Count, "Gpi bindings count mismatch");

            for (int i = 0; i < gpi.Bindings.Count; ++i)
            {
                Assert.AreEqual(gpi.Bindings[i].DeviceId, deserialized.Bindings[i].DeviceId, "Different gpi binding value");
                Assert.AreEqual(gpi.Bindings[i].PortNumber, deserialized.Bindings[i].PortNumber, "Different gpi binding value");
                Assert.AreEqual(gpi.Bindings[i].PinNumber, deserialized.Bindings[i].PinNumber, "Different gpi binding value");                
            }
        }
    }
}
