//#undef DEBUG

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
using System.Collections.ObjectModel;
using System.Diagnostics;
using TAS.Server;
using TAS.Client.ViewModels;
using System.Threading;
using TAS.Common;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using TAS.Client.Common;

namespace TAS.Client
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
    }
}
