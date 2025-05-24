using System;
using System.Globalization;

namespace TAS.Server
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class BuildDateAttribute : Attribute
    {
        public BuildDateAttribute(string value)
        {
            BuildDate = DateTime.ParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        public DateTime BuildDate { get; }
    }
}
