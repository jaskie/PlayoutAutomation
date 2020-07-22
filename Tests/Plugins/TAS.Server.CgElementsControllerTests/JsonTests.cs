using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TAS.Common.Interfaces;
using TAS.Database.Common;
using TAS.Server.CgElementsController.Configurator.Model;

namespace TAS.Server.CgElementsControllerTests
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
        
        private static IEnumerable<object[]> GetCgElementsControllers()
        {
            foreach(var cgElementsController in CgElementsControllerTestData.CgElementsControllers)
                yield return new object[] { cgElementsController };            
        }        
        #endregion

        [TestMethod]
        [DynamicData(nameof(GetCgElementsControllers), DynamicDataSourceType.Method)]
        public void SerializeAndDeserialize(ICGElementsController cgElementsController)
        {
            var json = JsonConvert.SerializeObject(cgElementsController, _jsonSerializerSettings);
            var deserialized = JsonConvert.DeserializeObject<CgElementsController.CgElementsController>(json, _jsonSerializerSettings);

            if (deserialized == null && cgElementsController != null)
                Assert.Fail("Failed to deserialize CgElementsController");
            
            if (deserialized == null)
                return;

            Assert.AreEqual(cgElementsController.IsEnabled, deserialized.IsEnabled);
            Assert.AreEqual(cgElementsController.DefaultCrawl, deserialized.DefaultCrawl);
            Assert.AreEqual(cgElementsController.DefaultLogo, deserialized.DefaultLogo);

            var deserializedCgCollection = new List<ICGElement>();
            var actualCgCollection = new List<ICGElement>();

            try
            {               
                if (cgElementsController.Parentals != null)
                {
                    actualCgCollection.Concat(cgElementsController.Parentals);
                    deserializedCgCollection.Concat(deserialized.Parentals);
                }

                if (cgElementsController.Crawls != null)
                {
                    actualCgCollection.Concat(cgElementsController.Crawls);
                    deserializedCgCollection.Concat(deserialized.Crawls);
                }

                if (cgElementsController.Logos != null)
                {
                    actualCgCollection.Concat(cgElementsController.Logos);
                    deserializedCgCollection.Concat(deserialized.Logos);
                }
            }
            catch(Exception ex)
            {
                Assert.Fail("CgElementsController's CgCollections failed to deserialize properly. {o}", ex.Message);
            }
           
            for(int i=0; i<actualCgCollection.Count; ++i)
            {
                Assert.AreEqual(((CgElement)actualCgCollection[i]).Id, ((CgElement)deserializedCgCollection[i]).Id, "CgElement value is different");
                Assert.AreEqual(((CgElement)actualCgCollection[i]).Command, ((CgElement)deserializedCgCollection[i]).Command, "CgElement value is different");
                Assert.AreEqual(((CgElement)actualCgCollection[i]).Name, ((CgElement)deserializedCgCollection[i]).Name, "CgElement value is different");
                Assert.AreEqual(((CgElement)actualCgCollection[i]).ServerImagePath, ((CgElement)deserializedCgCollection[i]).ServerImagePath, "CgElement value is different");
                Assert.AreEqual(((CgElement)actualCgCollection[i]).ClientImagePath, ((CgElement)deserializedCgCollection[i]).ClientImagePath, "CgElement value is different");
                Assert.AreEqual(((CgElement)actualCgCollection[i]).ImageFile, ((CgElement)deserializedCgCollection[i]).ImageFile, "CgElement value is different");                
            }
        }
    }
}
