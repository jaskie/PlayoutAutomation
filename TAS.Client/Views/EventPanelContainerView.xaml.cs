
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
      ApplySettings();
    }
    void ApplySettings()
    {
      var containerNameHeihgt = GetContainerNameHeight();
      UISettings.Apply(lbContenerName);

      var heightDiff = GetContainerNameHeight() - containerNameHeihgt - 5;
      if (heightDiff > 0)
      {
        mainGrid.RowDefinitions[0].Height = new System.Windows.GridLength(mainGrid.RowDefinitions[0].Height.Value + heightDiff);
      }
    }
    double GetContainerNameHeight()
    {
      lbContenerName.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
      return lbContenerName.DesiredSize.Height;
    }
  }

}
