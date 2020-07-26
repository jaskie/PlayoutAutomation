using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using TAS.Common.Interfaces.Configurator;
using TAS.Database.Common;
using TAS.Database.Common.Interfaces;
using TestData;

namespace SerializationTests
{
    public class ConfigurationPluginManager
    {
        private const string FileNameSearchPattern = "TAS.Server.*.dll";
        private ConfigurationPluginManager()
        {
            string pluginsPath = "../../../../TVPlay/bin/Debug/Plugins";
#if RELEASE
            pluginsPath = "../../../../TVPlay/bin/Release/Plugins";
#endif          
            using (var catalog = new DirectoryCatalog(pluginsPath, FileNameSearchPattern))
            using (var container = new CompositionContainer(catalog))
            {
                PluginTypeBinders = container.GetExportedValues<IPluginTypeBinder>();
            }
        }

        [ImportMany(typeof(IPluginTypeBinder))]
        public IEnumerable<IPluginTypeBinder> PluginTypeBinders { get; }


        public static ConfigurationPluginManager Current { get; } = new ConfigurationPluginManager();
    }

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

            Assert.AreEqual(configEngine.Gpis.Count, deserialized.Gpis.Count);
            
        }
    }
}
