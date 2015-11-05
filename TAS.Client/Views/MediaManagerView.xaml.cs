using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TAS.Client.ViewModels;
using System.Collections;

namespace TAS.Client.Views
{
    /// <summary>
    /// Interaction logic for ServerManager.xaml
    /// </summary>
    public partial class MediaManagerView : UserControl
    {
        public MediaManagerView()
        {
            InitializeComponent();
        }

        private MediaManagerViewmodel _mediaManagerViewModel;
        public MediaManagerViewmodel MediaManagerViewmodel
        {
            get { return _mediaManagerViewModel; }
            set
            {
                if (_mediaManagerViewModel != value)
                {
                    _mediaManagerViewModel = value;
                    DataContext = _mediaManagerViewModel;
                }
            }
        }

        private PreviewViewmodel _previewViewModel;
        public PreviewViewmodel PreviewViewmodel
        {
            get { return _previewViewModel; }
            set
            {
                if (_previewViewModel != value)
                {
                    _previewViewModel = value;
                }
            }
        }

        private void toggleButton_Click(object sender, RoutedEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while (!(obj is DataGridRow) && obj != null)
                obj = VisualTreeHelper.GetParent(obj);

            if (obj is DataGridRow)
                if ((obj as DataGridRow).DetailsVisibility == Visibility.Visible)
                    (obj as DataGridRow).DetailsVisibility = Visibility.Collapsed;
                else
                    (obj as DataGridRow).DetailsVisibility = Visibility.Visible;
        }
    }
}
