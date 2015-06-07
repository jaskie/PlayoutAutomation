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

namespace TAS.Client
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
