using System.Windows.Controls;

namespace TAS.Client.Views
{
    /// <summary>
    /// Interaction logic for EventPanel.xaml
    /// </summary>
    public partial class EventPanelRundownView
    {
        public EventPanelRundownView()
        {
            InitializeComponent();
            ApplySettings();
            _resizer = new EventPanelResizer(mainGrid, lbOffset);
        }
        readonly EventPanelResizer _resizer;
        void ApplySettings()
        {
            var rundownNameHeihgt = GetRundownNameHeight();
            UISettings.Apply(lbRundownName);

            UISettings.Apply(lbTimeLeft);
            UISettings.ApplyToEventTime(lbScheduleTime);
            UISettings.ApplyToEventTime(lbOffset);
            UISettings.ApplyToEventTime(lbDuration);
            UISettings.ApplyToEventTime(lbEndTime);

            var heightDiff = GetRundownNameHeight() - rundownNameHeihgt - 5;
            if (heightDiff > 0)
            {
                mainGrid.RowDefinitions[0].Height = new System.Windows.GridLength(mainGrid.RowDefinitions[0].Height.Value + heightDiff);
            }
        }
        double GetRundownNameHeight()
        {
            lbRundownName.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            return lbRundownName.DesiredSize.Height;
        }
    }

}
