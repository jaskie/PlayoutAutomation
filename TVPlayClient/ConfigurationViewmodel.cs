using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Xml.Serialization;
using TAS.Client.Common;
using TAS.Client.ViewModels;

namespace TVPlayClient
{
    public class ConfigurationViewmodel: ViewmodelBase
    {
        private readonly ObservableCollection<ConfigurationChannel> _channels;
        private readonly string _configurationFile;
        public ConfigurationViewmodel(string configurationFile)
        {
            _configurationFile = configurationFile;
            if (File.Exists(configurationFile))
            {
                XmlSerializer reader = new XmlSerializer(typeof(ObservableCollection<ConfigurationChannel>), new XmlRootAttribute("Channels"));
                using (StreamReader file = new StreamReader(configurationFile))
                    _channels = (ObservableCollection<ConfigurationChannel>)reader.Deserialize(file);
            }
            else
                _channels = new ObservableCollection<ConfigurationChannel>();
            CommandAdd = new UICommand { ExecuteDelegate = _add };
            CommandDelete = new UICommand { ExecuteDelegate = _delete, CanExecuteDelegate = _canDelete };
            CommandSave = new UICommand { ExecuteDelegate = _save };
            CommandCancel = new UICommand { ExecuteDelegate = _cancel };
        }

        private void _cancel(object obj)
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void _save(object obj)
        {
            var directoryName = Path.GetDirectoryName(_configurationFile);
            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);
            XmlSerializer writer = new XmlSerializer(typeof(ObservableCollection<ConfigurationChannel>), new XmlRootAttribute("Channels"));
            using (StreamWriter file = new StreamWriter(_configurationFile))
                writer.Serialize(file, Channels);
            Closed?.Invoke(this, EventArgs.Empty);
        }

        private bool _canDelete(object obj)
        {
            return _selectedChannel != null;
        }

        private void _delete(object obj)
        {
            _channels.Remove(_selectedChannel);
        }

        private void _add(object obj)
        {
            _channels.Add(new ConfigurationChannel() { Address = "127.0.0.1:1061" });
        }

        public IEnumerable<ConfigurationChannel> Channels { get { return _channels; } }

        private ConfigurationChannel _selectedChannel;
        public ConfigurationChannel SelectedChannel { get { return _selectedChannel; } set { SetField(ref _selectedChannel, value, nameof(SelectedChannel)); } }
        
        public ICommand CommandSave { get; private set; }
        public ICommand CommandAdd { get; private set; }
        public ICommand CommandDelete { get; private set; }
        public ICommand CommandCancel { get; private set; }

        public event EventHandler Closed;

        protected override void OnDispose()
        {
            
        }
    }

    [XmlType("Channel")]
    public class ConfigurationChannel
    {
        [XmlAttribute]
        public string Address { get; set; }
        [XmlAttribute]
        public bool AllowControl { get; set; } = true;
        [XmlAttribute]
        public bool ShowEngine { get; set; } = true;
        [XmlAttribute]
        public bool ShowMedia { get; set; } = true;
    }
}
