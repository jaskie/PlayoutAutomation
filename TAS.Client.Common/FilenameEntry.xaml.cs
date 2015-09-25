using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;


namespace TAS.Client
{
    /// <summary>
    /// Interaction logic for FilenameEntry.xaml
    /// </summary>
    public partial class FilenameEntry : UserControl
    {
        public static DependencyProperty FileNameProperty = DependencyProperty.Register("FileName", typeof(string), typeof(FilenameEntry), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static DependencyProperty DialogTitleProperty = DependencyProperty.Register("DialogTitle", typeof(string), typeof(FilenameEntry), new PropertyMetadata(null));
        public static DependencyProperty DialogFilterProperty = DependencyProperty.Register("DialogFilterTitle", typeof(string), typeof(FilenameEntry), new PropertyMetadata(null));

        public string FileName { get { return GetValue(FileNameProperty) as string; } set { SetValue(FileNameProperty, value); } }

        public string DialogTitle { get { return GetValue(DialogTitleProperty) as string; } set { SetValue(DialogTitleProperty, value); } }

        public string DialogFilter { get { return GetValue(DialogFilterProperty) as string; } set { SetValue(DialogFilterProperty, value); } }

        public FilenameEntry() { InitializeComponent(); }

        private void BrowseFolder(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Title = DialogTitle;
            dlg.FileName = FileName;
            if (dlg.ShowDialog() == true)
            {
                FileName = dlg.FileName;
                BindingExpression be = GetBindingExpression(FileNameProperty);
                if (be != null)
                    be.UpdateSource();
            }
        }
    }
}
    