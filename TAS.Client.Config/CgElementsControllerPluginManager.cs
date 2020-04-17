using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Xml;
using System.Xml.Serialization;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Client.Config.Model.Plugins;

namespace TAS.Client.Config
{
    public class CgElementsControllerPluginManager : ViewModelBase, IPluginManager
    {
        public CgElementsControllerViewModel CgElementsControllerVM { get; private set; }
        private readonly List<Engine> _engines;
        private Engine _selectedEngine;  
        
        [XmlArray("CgElementsControllers")]
        [XmlArrayItem("CgElementsController")]
        private List<CgElementsController> _cgElementsControllers;

        public CgElementsControllerPluginManager(Engines engines)
        {
            LoadConfiguration();
            _engines = engines.EngineList;
                        
            Engines = CollectionViewSource.GetDefaultView(_engines);
        }

        private void LoadConfiguration()
        {
            try
            {
                using (StreamReader reader = new StreamReader("Configuration\\CgElementsControllers.xml"))
                {
                    XmlRootAttribute xRoot = new XmlRootAttribute();
                    xRoot.ElementName = "CgElementsControllers";

                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<CgElementsController>), xRoot);
                    _cgElementsControllers = (List<CgElementsController>)xmlSerializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                _cgElementsControllers = new List<CgElementsController>();
                if (ex is InvalidOperationException xmlException)
                    MessageBox.Show("Error while reading configuration file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                using (StreamWriter reader = new StreamWriter("Configuration\\Test.xml"))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<CgElementsController>));
                    xmlSerializer.Serialize(reader, _cgElementsControllers);
                }
            }
        }

        public ICollectionView Engines { get; }
        public string PluginName => "CgElementsController";
        public Engine SelectedEngine 
        { 
            get => _selectedEngine;
            set
            {
                if (!SetField(ref _selectedEngine, value))
                    return;

                CgElementsControllerVM = new CgElementsControllerViewModel(_cgElementsControllers.FirstOrDefault(cg => cg.EngineName == value.EngineName));
            }
        }

        protected override void OnDispose()
        {
            //
        }
    }
}
