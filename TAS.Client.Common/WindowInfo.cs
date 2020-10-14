using System.Windows;

namespace TAS.Client.Common
{
    public class WindowInfo
    {
        public WindowStartupLocation? WindowStartupLocation { get; set; }
        public SizeToContent? SizeToContent { get; set; }
        public string Title { get; set; }
        public bool? ShowInTaskbar { get; set; }
        public bool? AllowTransparency { get; set; }        
        public ResizeMode? ResizeMode { get; set; }
        public Window Owner { get; set; }
    }
}
