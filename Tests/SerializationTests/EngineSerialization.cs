using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.IO;
using TAS.Client.Config;
using TAS.Common.Interfaces.Configurator;
using TAS.Database.Common;
using TAS.Database.Common.Interfaces;
using TAS.Server;
using TestData;

namespace SerializationTests
{   
    [TestClass]
    public class EngineSerialization
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

        private static IEnumerable<object[]> GetConfigEngine()
        {
            foreach (var engine in TestEngines.ConfigEngines)
                yield return new object[] { engine };
        }
#endregion

        [ClassInitialize]
        public static void TestInitialize(TestContext testContext)
        {
            string pluginsPath = "../../../../TVPlay/bin/Debug/";
#if RELEASE
            pluginsPath = "../../../../TVPlay/bin/Release/";
#endif

            Directory.SetCurrentDirectory(pluginsPath);

            //ConfigurationManager.AppSettings["DatabaseType"] = "MySQL";
            //var section = (ConnectionStringsSection)ConfigurationManager.GetSection("connectionStrings");
            ////var connectionSettings = new ConnectionStringSettingsCollection();
            ////connectionSettings.Add(new ConnectionStringSettings("tasConnectionString", "server = localhost; database = tas2; user id = root; keepalive = 300; characterset = utf8; password = haslo; sslmode = Prefered; port = 3306"));
            //section.ConnectionStrings.Add(new ConnectionStringSettings("tasConnectionString", "server = localhost; database = tas2; user id = root; keepalive = 300; characterset = utf8; password = haslo; sslmode = Prefered; port = 3306"));
        }

        [TestMethod]
        [DynamicData(nameof(GetConfigEngine), DynamicDataSourceType.Method)]
        public void SerializeAndDeserialize(IConfigEngine configEngine)
        {
            _jsonSerializerSettings.SerializationBinder = new HibernationSerializationBinder(ConfigurationPluginManager.Current.PluginTypeBinders);
            var json = JsonConvert.SerializeObject(configEngine, _jsonSerializerSettings);

            _jsonSerializerSettings.SerializationBinder = null;
            var deserialized = JsonConvert.DeserializeObject<TAS.Server.Engine>(json, _jsonSerializerSettings);

            if (deserialized == null && configEngine != null)
                Assert.Fail("Failed to deserialize CgElementsController");

            if (deserialized == null)
                return;

            Assert.AreEqual(configEngine.EngineName, deserialized.EngineName);
            Assert.AreEqual(configEngine.CGStartDelay, deserialized.CGStartDelay);
            Assert.AreEqual(configEngine.AspectRatioControl, deserialized.AspectRatioControl);
            Assert.AreEqual(configEngine.CrawlEnableBehavior, deserialized.CrawlEnableBehavior);
            Assert.AreEqual(configEngine.EnableCGElementsForNewEvents, deserialized.EnableCGElementsForNewEvents);
            Assert.AreEqual(configEngine.StudioMode, deserialized.StudioMode);
            Assert.AreEqual(configEngine.TimeCorrection, deserialized.TimeCorrection);
            Assert.AreEqual(configEngine.VideoFormat, deserialized.VideoFormat);

            Assert.IsTrue(configEngine.CGElementsController?.IsEnabled ?? false ? deserialized.CGElementsController != null : deserialized.CGElementsController == null);
            Assert.IsTrue(configEngine.Router?.IsEnabled ?? false ? deserialized.Router != null : deserialized.Router == null);

            Assert.AreEqual(configEngine.Gpis?.Count, deserialized.Gpis?.Count, "Gpis did not deserialize properly");
            
        }
    }
}
