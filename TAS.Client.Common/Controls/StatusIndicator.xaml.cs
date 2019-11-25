using System.Windows;
using System.Windows.Controls;

namespace TAS.Client.Common.Controls
{
    /// <summary>
    /// Interaction logic for RedGreenIndicator.xaml
    /// </summary>
    public partial class StatusIndicator
       : UserControl
    {
        public static readonly DependencyProperty IsGreenProperty = DependencyProperty.Register("IsGreen", typeof(bool), typeof(StatusIndicator), new UIPropertyMetadata(false));

        public bool IsGreen
        {
            get
            {
                return (bool)GetValue(IsGreenProperty);
            }
            set
            {
                SetValue(IsGreenProperty, value);
            }
        }

        public StatusIndicator()
        {
            InitializeComponent();
        }
    }
}
