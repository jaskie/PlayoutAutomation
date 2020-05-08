using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Xml.Serialization;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Client.Config.Model.Plugins;

namespace TAS.Client.Config.ViewModels.Plugins.CgElementsController
{
    public class CgElementsControllerPluginManager : ViewModelBase, IPluginManager
    {
        private CgElementsControllerViewModel _cgElementsControllerVm;
        
        private readonly List<Engine> _engines;
        private Engine _selectedEngine;  
        
        [XmlArray("CgElementsControllers")]
        [XmlArrayItem("CgElementsController")]
        private List<Model.CgElementsController> _cgElementsControllers;

        public CgElementsControllerPluginManager(Model.Engines engines)
        {
            LoadConfiguration();
            _engines = engines.EngineList;
                        
            Engines = CollectionViewSource.GetDefaultView(_engines);
        }

        private void CgElementsControllerUpdated(object sender, EventArgs e)
        {                    
            using (StreamWriter writer = new StreamWriter("Configuration\\CgElementsControllers.xml"))
            {
                XmlRootAttribute xRoot = new XmlRootAttribute();
                xRoot.ElementName = "CgElementsControllers";

                XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<Model.CgElementsController>), xRoot);
                xmlSerializer.Serialize(writer, _cgElementsControllers);
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                using (StreamReader reader = new StreamReader("Configuration\\CgElementsControllers.xml"))
                {
                    XmlRootAttribute xRoot = new XmlRootAttribute();
                    xRoot.ElementName = "CgElementsControllers";

                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<Model.CgElementsController>), xRoot);
                    _cgElementsControllers = (List<Model.CgElementsController>)xmlSerializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                _cgElementsControllers = new List<Model.CgElementsController>();
                if (ex is InvalidOperationException xmlException)
                    MessageBox.Show("Error while reading configuration file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                using (StreamWriter reader = new StreamWriter("Configuration\\Test.xml"))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<Model.CgElementsController>));
                    xmlSerializer.Serialize(reader, _cgElementsControllers);
                }
            }
        }

        public ICollectionView Engines { get; }
        public string PluginName => "CgElementsController";

        public CgElementsControllerViewModel CgElementsControllerVm
        {
            get => _cgElementsControllerVm;
            private set
            {
                var old = _cgElementsControllerVm;
                if (!SetField(ref _cgElementsControllerVm, value))
                    return;
                
                if (old != null)
                    old.DataUpdated -= CgElementsControllerUpdated;
                if (value != null)
                    _cgElementsControllerVm.DataUpdated += CgElementsControllerUpdated;

            }
        }        

        public Engine SelectedEngine 
        { 
            get => _selectedEngine;
            set
            {
                if (!SetField(ref _selectedEngine, value))
                    return;

                var cgElementContoller = _cgElementsControllers.FirstOrDefault(cg => cg.EngineName.Replace("_Disabled", "") == value.EngineName);
                if (cgElementContoller == null)
                {
                    cgElementContoller = new Model.CgElementsController { EngineName = value.EngineName };
                    _cgElementsControllers.Add(cgElementContoller);
                }

                CgElementsControllerVm = new CgElementsControllerViewModel(cgElementContoller);
            }
        }

        protected override void OnDispose()
        {
            //
        }
    }
}
