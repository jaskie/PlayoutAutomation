using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using TAS.Client.Common;
using TAS.Client.ViewModels;

namespace TVPlayClient
{
    public class MainWindowViewmodel : ViewmodelBase
    {
        public MainWindowViewmodel()
        {
            Application.Current.Dispatcher.ShutdownStarted += _dispatcher_ShutdownStarted;
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
                _loadTabs();
            CommandConfigure = new UICommand { ExecuteDelegate = _configure };
        }

        private void _configure(object obj)
        {
            (_content as ChannelsViewmodel)?.Dispose();
            var vm = new ConfigurationViewmodel();
            vm.Closed += _configClosed;
            ShowConfigButton = false;
            Content = vm;
        }

        private void _configClosed(object sender, EventArgs e)
        {
            (sender as ConfigurationViewmodel).Dispose();
            ShowConfigButton = true;
            _loadTabs();
        }

        private void _dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            Dispose();
        }

        protected override void OnDispose()
        {
            (_content as ChannelsViewmodel)?.Dispose();
        }

        private void _loadTabs()
        {
            string configurationFile = ConfigurationManager.AppSettings[ConfigurationViewmodel.ConfigurationFileKey];
            if (string.IsNullOrWhiteSpace(configurationFile))
                configurationFile = ConfigurationViewmodel.DefaultConfigurationFile;
            if (File.Exists(configurationFile))
            {
                XmlSerializer reader = new XmlSerializer(typeof(List<ChannelWrapperViewmodel>), new XmlRootAttribute("Channels"));
                using (StreamReader file = new StreamReader(configurationFile))
                    Content = new ChannelsViewmodel((List<ChannelWrapperViewmodel>)reader.Deserialize(file));
            }
        }

        private ViewmodelBase _content;
        public ViewmodelBase Content { get { return _content; } private set { SetField(ref _content, value, nameof(Content)); } }

        public ICommand CommandConfigure { get; private set; }
        private bool _showConfigButton = true;
        public bool ShowConfigButton { get { return _showConfigButton; }  private set { SetField(ref _showConfigButton, value, nameof(ShowConfigButton)); } }
        
    }
}
