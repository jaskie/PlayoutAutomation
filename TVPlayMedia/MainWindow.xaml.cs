using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TAS.Client.ViewModels;

namespace TAS.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly Model.RemoteClient _client;
        public MainWindow()
        {
#if DEBUG
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            System.Threading.Thread.Sleep(5000); // wait for server to set up
#endif
            InitializeComponent();
            try
            {
                _client = new Model.RemoteClient(ConfigurationManager.AppSettings["Host"]);
                    _client.CreationConverters = new Newtonsoft.Json.JsonConverter[] {
                    new Converters.IMediaConverter(_client),
                    new Converters.IFileOperationConverter(_client),
                    new Converters.IMediaSegmentConverter(_client),
                    new Converters.IMediaDirectoryConverter(_client),
                    new Converters.IFileManagerConverter(_client),
                    new Converters.MediaManagerConverter(_client)
                    };
                _client.Initialize();
                Model.MediaManager mm = _client.GetInitalObject<Model.MediaManager>();
                MediaManagerViewmodel vm = new MediaManagerViewmodel(mm, null);
                _windowContent.Content = vm.View;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }
}
