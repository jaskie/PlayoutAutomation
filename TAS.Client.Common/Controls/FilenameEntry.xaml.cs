using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;


namespace TAS.Client.Common.Controls
{
    /// <summary>
    /// Interaction logic for FilenameEntry.xaml
    /// </summary>
    public partial class FilenameEntry : UserControl
    {
        public static DependencyProperty FileNameProperty = DependencyProperty.Register("FileName", typeof(string), typeof(FilenameEntry), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static DependencyProperty DialogTitleProperty = DependencyProperty.Register("DialogTitle", typeof(string), typeof(FilenameEntry), new PropertyMetadata(null));
        public static DependencyProperty DialogFilterProperty = DependencyProperty.Register("DialogFilter", typeof(string), typeof(FilenameEntry), new PropertyMetadata(null));
        public static DependencyProperty CheckFileExistsProperty = DependencyProperty.Register("CheckFileExists", typeof(bool), typeof(FilenameEntry), new PropertyMetadata(false));
        public static DependencyProperty ButtonToolTipProperty = DependencyProperty.Register("ButtonToolTip", typeof(string), typeof(FilenameEntry), new PropertyMetadata(null));
        public static DependencyProperty InitialDirectoryProperty = DependencyProperty.Register("InitialDirectory", typeof(string), typeof(FilenameEntry), new PropertyMetadata(null));

        public string FileName { get => (string) GetValue(FileNameProperty); set => SetValue(FileNameProperty, value); }
        public string DialogTitle { get => (string) GetValue(DialogTitleProperty); set => SetValue(DialogTitleProperty, value); }
        public string DialogFilter { get => (string )GetValue(DialogFilterProperty) ; set => SetValue(DialogFilterProperty, value); }
        public bool CheckFileExists { get => (bool) GetValue(CheckFileExistsProperty); set => SetValue(CheckFileExistsProperty, value); }
        public string ButtonToolTip { get => (string) GetValue(ButtonToolTipProperty); set => SetValue(ButtonToolTipProperty, value); }
        public string InitialDirectory { get => (string) GetValue(InitialDirectoryProperty); set => SetValue(InitialDirectoryProperty, value); }

        public FilenameEntry() { InitializeComponent(); }

        private void BrowseFolder(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = DialogTitle,
                FileName = FileName,
                Filter = DialogFilter,
                CheckFileExists = CheckFileExists,
                InitialDirectory = InitialDirectory
            };
            if (dlg.ShowDialog() != true)
                return;
            FileName = string.IsNullOrWhiteSpace(InitialDirectory)
                ? dlg.FileName
                : Uri.UnescapeDataString(
                    new Uri(InitialDirectory.EndsWith(Path.DirectorySeparatorChar.ToString())
                            ? InitialDirectory
                            : InitialDirectory + Path.DirectorySeparatorChar).MakeRelativeUri(new Uri(dlg.FileName))
                        .ToString().Replace('/', Path.DirectorySeparatorChar));
            var be = GetBindingExpression(FileNameProperty);
            be?.UpdateSource();
        }
    }
}
    