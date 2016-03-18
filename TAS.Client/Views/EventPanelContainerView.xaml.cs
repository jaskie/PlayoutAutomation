
using System.Diagnostics;

namespace TAS.Client.Views
{
    /// <summary>
    /// Interaction logic for EventPanel.xaml
    /// </summary>
    /// 

    public partial class EventPanelContainerView : EventPanelView
    {
        public EventPanelContainerView()
        {
            InitializeComponent();
        }

        private void grid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Debug.WriteLine(tb.Focus());
        }

        private void grid_PreviewLostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            Debug.WriteLine("Lost!");
        }
    }

}
