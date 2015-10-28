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
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using TAS.Client.ViewModels;
using TAS.Client.Common;
using TAS.Common;

namespace TAS.Client
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MediaSearchView : Window
    {
        public MediaSearchView(RationalNumber frameRate)
        {
            InitializeComponent();
            ((TimeSpanToSMPTEConverter)Resources["TimeSpanToSMPTE"]).FrameRate = frameRate;
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedMedia != null)
            {
                if (e.AddedItems.Count == 0)
                    _selectedMedia.SelectedSegment = null;
                else
                    if (e.AddedItems[0] is MediaSegmentViewmodel 
                        && e.AddedItems[0] != _selectedMedia.SelectedSegment
                        && (e.AddedItems[0] as MediaSegmentViewmodel).Media == _selectedMedia.Media )
                        _selectedMedia.SelectedSegment = (MediaSegmentViewmodel)e.AddedItems[0];
                    else
                        _selectedMedia.SelectedSegment = null;
            }
        }

        private MediaViewViewmodel _selectedMedia;
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is MediaViewViewmodel)
            {
                _selectedMedia = (MediaViewViewmodel)e.AddedItems[0];
                (sender as DataGrid).ScrollIntoView(e.AddedItems[0]);
            }

        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void tbSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && gSearch.SelectedIndex < gSearch.Items.Count)
                gSearch.SelectedIndex++;
            if (e.Key == Key.Up && gSearch.SelectedIndex > 0)
                gSearch.SelectedIndex--;
        }
        
    }
}
