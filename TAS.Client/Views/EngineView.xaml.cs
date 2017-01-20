//#undef DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using TAS.Common;
using TAS.Client.Common;

namespace TAS.Client.Views
{
    /// <summary>
    /// Interaction logic for Channel.xaml
    /// </summary>
    /// 
    public partial class EngineView : UserControl
    {
        public EngineView(RationalNumber frameRate)
        {
            InitializeComponent();
            ((TimeSpanToSMPTEConverter)Resources["TimeSpanToSMPTE"]).FrameRate = frameRate;
        }

        public EngineView()
        {
            InitializeComponent();
        }

        private RationalNumber _frameRate;
        public RationalNumber FrameRate
        {
            get { return _frameRate; }
            set
            {
                if (value != _frameRate)
                {
                    ((TimeSpanToSMPTEConverter)Resources["TimeSpanToSMPTE"]).FrameRate = value;
                    _frameRate = value;
                }
            }
        }

    }
}
