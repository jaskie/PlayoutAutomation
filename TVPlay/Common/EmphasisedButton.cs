using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace TAS.Client
{
    class EmphasisedButton : Button
    {
        private bool _isEmphasised = false;
        public bool IsEmphasised
        {
            get { return _isEmphasised; }
            set
            {
                if (value != _isEmphasised)
                {
                    _isEmphasised = value;
                    if (_isEmphasised)
                        Background = Brushes.PaleGreen;
                    else
                        ClearValue(BackgroundProperty);
                }
            }
        }
        public static readonly DependencyProperty IsEmphasisedProperty =
        DependencyProperty.Register(
            "IsEmphasised",
            typeof(bool),
            typeof(EmphasisedButton),
            new PropertyMetadata(false, OnItemsPropertyChanged));
        private static void OnItemsPropertyChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EmphasisedButton source = d as EmphasisedButton;
            source.IsEmphasised = (bool)e.NewValue;
        }

    }
}
