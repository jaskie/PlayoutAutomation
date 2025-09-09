using System;
using System.Xml.Serialization;

namespace TAS.Common.Helpers
{
    public class XmlTimeSpan
    {
        private TimeSpan _value;

        public XmlTimeSpan() { }

        public XmlTimeSpan(TimeSpan value) { _value = value; }

        public static implicit operator TimeSpan?(XmlTimeSpan o)
        {
            return o == null ? default(TimeSpan?) : o._value;
        }

        public static implicit operator XmlTimeSpan(TimeSpan? o)
        {
            return o == null ? null : new XmlTimeSpan(o.Value);
        }

        public static implicit operator TimeSpan(XmlTimeSpan o)
        {
            return o == null ? default : o._value;
        }

        public static implicit operator XmlTimeSpan(TimeSpan o)
        {
            return o == default ? null : new XmlTimeSpan(o);
        }

        [XmlText]
        public string Default
        {
            get { return _value.ToString(); }
            set { _value = TimeSpan.Parse(value); }
        }
    }
}
