using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TAS.Client.Common
{
    public class WindowInfo
    {
        public WindowStartupLocation WindowStartupLocation { get; set; }
        public SizeToContent SizeToContent { get; set; }
        public string Title { get; set; }
        public bool ShowInTaskbat { get; set; }
        public bool AllowTransparency { get; set; }
    }
}
