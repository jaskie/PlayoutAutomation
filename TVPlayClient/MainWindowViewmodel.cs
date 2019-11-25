using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using TAS.Client.Common;
using TAS.Common;

namespace TVPlayClient
{
    public class MainWindowViewmodel : ViewModelBase
    {
        private const string ConfigurationFileName = "Channels.xml";
        private readonly string _configurationFile;
        private ViewModelBase _content;
        private bool _showConfigButton = true;

        public MainWindowViewmodel()
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;
            Application.Current.Dispatcher.ShutdownStarted += _dispatcher_ShutdownStarted;
            _configurationFile = Path.Combine(FileUtils.LocalApplicationDataPath, ConfigurationFileName);
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                _loadTabs();
            CommandConfigure = new UiCommand(_configure);
        }

        public ICommand CommandConfigure { get; }

        public ViewModelBase Content { get => _content; private set => SetField(ref _content, value); }

        public bool ShowConfigButton { get => _showConfigButton; private set => SetField(ref _showConfigButton, value); }

        protected override void OnDispose()
        {
            (_content as ChannelsViewmodel)?.Dispose();
        }

        private void _configure(object obj)
        {
            (_content as ChannelsViewmodel)?.Dispose();
            var vm = new ConfigurationViewmodel(_configurationFile);
            vm.Closed += _configClosed;
            ShowConfigButton = false;
            Content = vm;
        }

        private void _configClosed(object sender, EventArgs e)
        {
            if (sender is ConfigurationViewmodel vm)
            {
                vm.Closed -= _configClosed;
                vm.Dispose();
            }
            ShowConfigButton = true;
            _loadTabs();
        }

        private void _dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            Dispose();
        }

        private void _loadTabs()
        {
            if (!File.Exists(_configurationFile))
                return;
            var reader = new XmlSerializer(typeof(List<ConfigurationChannel>), new XmlRootAttribute("Channels"));
            using (var file = new StreamReader(_configurationFile))
                Content = new ChannelsViewmodel((List<ConfigurationChannel>)reader.Deserialize(file));
        }
        
    }
}
