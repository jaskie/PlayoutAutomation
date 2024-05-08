using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Xml.Serialization;
using TAS.Client.Common;

namespace TVPlayClient
{
    public class ConfigurationViewmodel: ViewModelBase
    {
        private readonly ObservableCollection<ChannelConfiguration> _channels;
        private readonly string _configurationFile;
        public ConfigurationViewmodel(string configurationFile)
        {
            _configurationFile = configurationFile;
            if (File.Exists(configurationFile))
            {
                XmlSerializer reader = new XmlSerializer(typeof(ObservableCollection<ChannelConfiguration>), new XmlRootAttribute("Channels"));
                using (StreamReader file = new StreamReader(configurationFile))
                    _channels = (ObservableCollection<ChannelConfiguration>)reader.Deserialize(file);
            }
            else
                _channels = new ObservableCollection<ChannelConfiguration>();
            CommandAdd = new UiCommand(CommandName(nameof(Add)), Add);
            CommandDelete = new UiCommand(CommandName(nameof(Delete)), Delete, CanDelete);
            CommandSave = new UiCommand(CommandName(nameof(Save)), Save);
            CommandCancel = new UiCommand(CommandName(nameof(Cancel)), Cancel);
        }

        private void Cancel(object _) => Closed?.Invoke(this, EventArgs.Empty);

        private void Save(object _)
        {
            var directoryName = Path.GetDirectoryName(_configurationFile);
            if (string.IsNullOrEmpty(directoryName))
                return;
            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);
            XmlSerializer writer = new XmlSerializer(typeof(ObservableCollection<ChannelConfiguration>), new XmlRootAttribute("Channels"));
            using (StreamWriter file = new StreamWriter(_configurationFile))
                writer.Serialize(file, Channels);
            Closed?.Invoke(this, EventArgs.Empty);
        }

        private bool CanDelete(object _) => _selectedChannel != null;

        private void Delete(object _) => _channels.Remove(_selectedChannel);

        private void Add(object _) => _channels.Add(new ChannelConfiguration { Address = "127.0.0.1:1061" });

        public IEnumerable<ChannelConfiguration> Channels => _channels;

        private ChannelConfiguration _selectedChannel;
        public ChannelConfiguration SelectedChannel
        {
            get => _selectedChannel;
            set => SetField(ref _selectedChannel, value);
        }
        
        public ICommand CommandSave { get; }
        public ICommand CommandAdd { get; }
        public ICommand CommandDelete { get; }
        public ICommand CommandCancel { get; }

        public event EventHandler Closed;

        protected override void OnDispose()
        {
            
        }
    }

    [XmlType("Channel")]
    public class ChannelConfiguration
    {
        [XmlAttribute]
        public string Address { get; set; }
        [XmlAttribute]
        public bool ShowEngine { get; set; } = true;
        [XmlAttribute]
        public bool ShowMedia { get; set; } = true;
    }
}
