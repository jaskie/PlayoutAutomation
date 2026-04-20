using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Client
{
    static class UISettings
    {
        static readonly int fontDelta = 0;
        static UISettings()
        {
            int.TryParse(System.Configuration.ConfigurationManager.AppSettings["UIFontDelta"], out fontDelta);
            fontDelta = Math.Min(Math.Max(0, fontDelta), MaxFontDelta);
        }
        public static void Apply(System.Windows.Controls.TextBlock control, int maxFontDelta)
        {
            control.FontSize=GetFontSize(control.FontSize, maxFontDelta);
        }
        public static void ApplyToEventTime(System.Windows.Controls.TextBlock control) => Apply(control, EventTimeMaxFontDelta);
        public static void Apply(System.Windows.Controls.TextBlock control) => Apply(control, MaxFontDelta);
        public static double GetFontSize(double baseSize) => GetFontSize(baseSize, MaxFontDelta);
        public static double GetFontSize(double baseSize, int maxFontDelta)
        {
            if (fontDelta > 0 && maxFontDelta > 0)
            {
                return baseSize + Math.Min(fontDelta, maxFontDelta);
            }
            return baseSize;
        }
        public const int EventTimeMaxFontDelta = 4;
        public const int MaxFontDelta = 50;
    }
}
