
namespace TAS.Client.Views
{
  /// <summary>
  /// Interaction logic for EventPanel.xaml
  /// </summary>
  /// 

  public partial class EventPanelLiveView : EventPanelView
  {
    public EventPanelLiveView()
    {
      InitializeComponent();
      ApplySettings();
      _resizer = new EventPanelResizer(mainGrid, lbOffset);
    }
    readonly EventPanelResizer _resizer;
    void ApplySettings()
    {
      var liveNameHeihgt = GetLiveNameHeight();
      UISettings.Apply(lbLiveEventName);

      UISettings.Apply(lbTimeLeft);
      UISettings.ApplyToEventTime(lbScheduleTime);
      UISettings.ApplyToEventTime(lbOffset);
      UISettings.ApplyToEventTime(lbDuration);
      UISettings.ApplyToEventTime(lbEndTime);

      var heightDiff = GetLiveNameHeight() - liveNameHeihgt - 5;
      if (heightDiff > 0)
      {
        mainGrid.RowDefinitions[0].Height = new System.Windows.GridLength(mainGrid.RowDefinitions[0].Height.Value + heightDiff);
      }
    }
    double GetLiveNameHeight()
    {
      lbLiveEventName.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
      return lbLiveEventName.DesiredSize.Height;
    }
  }

}
