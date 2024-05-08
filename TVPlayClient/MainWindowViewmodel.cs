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
#if DEBUG
            System.Threading.Thread.Sleep(2000); // wait for server startup
#endif
            Application.Current.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
            _configurationFile = Path.Combine(FileUtils.ApplicationDataPath, ConfigurationFileName);
            LoadTabs();
            CommandConfigure = new UiCommand(CommandName(nameof(Configure)), Configure);
        }

        public ICommand CommandConfigure { get; }

        public ViewModelBase Content { get => _content; private set => SetField(ref _content, value); }

        public bool ShowConfigButton { get => _showConfigButton; private set => SetField(ref _showConfigButton, value); }

        protected override void OnDispose()
        {
            (_content as ChannelsViewmodel)?.Dispose();
        }

        private void Configure(object obj)
        {
            (_content as ChannelsViewmodel)?.Dispose();
            var vm = new ConfigurationViewmodel(_configurationFile);
            vm.Closed += Config_Closed;
            ShowConfigButton = false;
            Content = vm;
        }

        private void Config_Closed(object sender, EventArgs e)
        {
            if (sender is ConfigurationViewmodel vm)
            {
                vm.Closed -= Config_Closed;
                vm.Dispose();
            }
            ShowConfigButton = true;
            LoadTabs();
        }

        private void Dispatcher_ShutdownStarted(object _, EventArgs e) => Dispose();

        private void LoadTabs()
        {
            if (!File.Exists(_configurationFile))
                return;
            var reader = new XmlSerializer(typeof(List<ChannelConfiguration>), new XmlRootAttribute("Channels"));
            using (var file = new StreamReader(_configurationFile))
                Content = new ChannelsViewmodel((List<ChannelConfiguration>)reader.Deserialize(file));
        }
        
    }
}
