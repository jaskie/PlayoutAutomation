
namespace TAS.Client.Views
{
    /// <summary>
    /// Interaction logic for EventPanel.xaml
    /// </summary>
    /// 

    public partial class EventPanelMovieView : EventPanelView
    {
        public EventPanelMovieView()
        {
            InitializeComponent();
            Loaded += EventPanelMovieView_Loaded;

        }

        private void EventPanelMovieView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ApplySettings();
            Loaded -= EventPanelMovieView_Loaded;
        }

        EventPanelResizer _resizer;
        void ApplySettings()
        {
            var nameHeihgt = GetNameHeight();
            UISettings.Apply(lbEventName);

            UISettings.Apply(lbTimeLeft);
            UISettings.ApplyToEventTime(lbScheduleTime);
            UISettings.ApplyToEventTime(lbOffset);
            UISettings.ApplyToEventTime(lbDuration);
            UISettings.ApplyToEventTime(lbEndTime);
            
            var heightDiff = GetNameHeight() - nameHeihgt;
            if (heightDiff > 0)
            {
                mainGrid.RowDefinitions[0].Height = new System.Windows.GridLength(mainGrid.RowDefinitions[0].Height.Value + heightDiff);
            }
            _resizer = new EventPanelResizer(mainGrid, lbOffset);
        }
        double GetNameHeight()
        {
            lbEventName.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            return lbEventName.DesiredSize.Height;
        }
    }

}
