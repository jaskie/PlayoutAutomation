using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;


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

        public string FileName { get { return GetValue(FileNameProperty) as string; } set { SetValue(FileNameProperty, value); } }
        public string DialogTitle { get { return GetValue(DialogTitleProperty) as string; } set { SetValue(DialogTitleProperty, value); } }
        public string DialogFilter { get { return GetValue(DialogFilterProperty) as string; } set { SetValue(DialogFilterProperty, value); } }
        public bool CheckFileExists { get { return (bool)GetValue(CheckFileExistsProperty); } set { SetValue(CheckFileExistsProperty, value); } }
        public string ButtonToolTip { get { return GetValue(ButtonToolTipProperty) as string; } set { SetValue(ButtonToolTipProperty, value); } }
        public string InitialDirectory { get { return GetValue(InitialDirectoryProperty) as string; } set { SetValue(InitialDirectoryProperty, value); } }

        public FilenameEntry() { InitializeComponent(); }

        private void BrowseFolder(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Title = DialogTitle;
            dlg.FileName = FileName;
            dlg.Filter = DialogFilter;
            dlg.CheckFileExists = CheckFileExists;
            dlg.InitialDirectory = InitialDirectory;
            if (dlg.ShowDialog() == true)
            {
                if (string.IsNullOrWhiteSpace(InitialDirectory))
                    FileName = dlg.FileName;
                else
                    FileName = Uri.UnescapeDataString(new Uri(InitialDirectory.EndsWith(Path.DirectorySeparatorChar.ToString())? InitialDirectory: InitialDirectory + Path.DirectorySeparatorChar ).MakeRelativeUri(new Uri(dlg.FileName)).ToString().Replace('/', Path.DirectorySeparatorChar));
                BindingExpression be = GetBindingExpression(FileNameProperty);
                if (be != null)
                    be.UpdateSource();
            }
        }
    }
}
    