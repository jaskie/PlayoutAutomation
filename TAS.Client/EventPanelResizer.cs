
namespace TAS.Client.Views
{
    class EventPanelResizer
    {
        public EventPanelResizer(System.Windows.Controls.Grid grid, System.Windows.Controls.TextBlock offsetLabel)
        {
            _grid = grid;
            _offsetLabel = offsetLabel;
            _offsetLabel.IsVisibleChanged += (s, e) => Resize();
            _baseSize = grid.RowDefinitions[0].Height.Value;
            Resize();
        }
        readonly System.Windows.Controls.Grid _grid;
        readonly System.Windows.Controls.TextBlock _offsetLabel;
        readonly double _baseSize;
        void Resize()
        {
            double newHeight = _baseSize;
            if (_offsetLabel.IsVisible)
            {
                _offsetLabel.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                newHeight = _baseSize + _offsetLabel.DesiredSize.Height;
            }
            _grid.RowDefinitions[0].Height = new System.Windows.GridLength(newHeight);
        }
    }
}
