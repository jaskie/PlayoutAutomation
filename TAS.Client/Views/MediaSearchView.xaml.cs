using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TAS.Client.ViewModels;

namespace TAS.Client.Views
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MediaSearchView : Window
    {
        public MediaSearchView()
        {
            InitializeComponent();
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedMedia == null)
                return;
            if (e.AddedItems.Count == 0)
                _selectedMedia.SelectedSegment = null;
            else
            if (e.AddedItems[0] is MediaSegmentViewmodel 
                && e.AddedItems[0] != _selectedMedia.SelectedSegment
                && (e.AddedItems[0] as MediaSegmentViewmodel)?.Media == _selectedMedia.Media )
                _selectedMedia.SelectedSegment = (MediaSegmentViewmodel)e.AddedItems[0];
            else
                _selectedMedia.SelectedSegment = null;
        }

        private MediaViewViewmodel _selectedMedia;
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0 || !(e.AddedItems[0] is MediaViewViewmodel sm))
                return;
            _selectedMedia = sm;
            (sender as DataGrid)?.ScrollIntoView(sm);
        }

        private void tbSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && gSearch.SelectedIndex < gSearch.Items.Count)
                gSearch.SelectedIndex++;
            if (e.Key == Key.Up && gSearch.SelectedIndex > 0)
                gSearch.SelectedIndex--;
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            if (TbSearch.Focus())
                TbSearch.SelectAll();
            if (System.Windows.Interop.ComponentDispatcher.IsThreadModal)
                DialogResult = true;
        }

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var vm = e.NewValue as MediaSearchViewmodel;
            Width = vm?.PreviewViewmodel == null ? 550 : 950;
        }

        private void BtnClose_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
