using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace TAS.Client.Common
{
    public static class WpfHacks
    {
        public static void ApplyGridViewRowPresenter_CellMargin()
        {
            
            FieldInfo gridViewCellMarginProperty = typeof(GridViewRowPresenter).GetField("_defalutCellMargin", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField);
            if (gridViewCellMarginProperty != null)
                gridViewCellMarginProperty.SetValue(null, new Thickness(3, 0, 3, 0));

        }
    }
}
