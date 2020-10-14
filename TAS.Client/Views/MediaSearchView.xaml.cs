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
            if (e.AddedItems[0] is MediaSegmentViewModel 
                && e.AddedItems[0] != _selectedMedia.SelectedSegment
                && (e.AddedItems[0] as MediaSegmentViewModel)?.Media == _selectedMedia.Media )
                _selectedMedia.SelectedSegment = (MediaSegmentViewModel)e.AddedItems[0];
            else
                _selectedMedia.SelectedSegment = null;
        }

        private MediaViewViewModel _selectedMedia;
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0 || !(e.AddedItems[0] is MediaViewViewModel sm))
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
            var vm = e.NewValue as MediaSearchViewModel;
            Width = vm?._preview == null ? 550 : 950;
        }

        private void BtnClose_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DataGrid_OnSorting(object sender, DataGridSortingEventArgs e)
        {
            if (!(sender is FrameworkElement fe) || !(fe.DataContext is MediaSearchViewModel vm))
                return;
            vm.UserSorted = true;
        }
    }
}
