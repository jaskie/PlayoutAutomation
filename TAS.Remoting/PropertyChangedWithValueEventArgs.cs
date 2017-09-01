using System.Collections;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;

namespace TAS.Remoting
{
    [JsonObject(IsReference = false)]
    public class PropertyChangedWithDataEventArgs : PropertyChangedEventArgs
    {
        protected PropertyChangedWithDataEventArgs(string propertyName): base(propertyName) { }

        public static PropertyChangedEventArgs Create(string propertyName, object value)
        {
            if (value is IEnumerable)
                return new PropertyChangedWithArrayEventArgs(propertyName, value);
            return new PropertyChangedWithValueEventArgs(propertyName, value);
        }

        public virtual object Value
        {
            get
            {
                return (this as PropertyChangedWithValueEventArgs)?.Value ??
                       (this as PropertyChangedWithArrayEventArgs)?.Value;
            }
            protected set { throw new System.NotImplementedException(); }
        }
    }

    [DebuggerDisplay("{PropertyName} = {Value}")]
    public class PropertyChangedWithValueEventArgs : PropertyChangedWithDataEventArgs
    {
        public PropertyChangedWithValueEventArgs(string propertyName, object value) : base(propertyName)
        {
            Value = value;
        }

        [JsonProperty]
        public override object Value { get; protected set; }
    }

    [DebuggerDisplay("{PropertyName} = {Value}")]
    public class PropertyChangedWithArrayEventArgs : PropertyChangedWithDataEventArgs
    {
        public PropertyChangedWithArrayEventArgs(string propertyName, object value) : base(propertyName)
        {
            Value = value;
        }

        [JsonProperty(ItemIsReference = true, TypeNameHandling = TypeNameHandling.All, ItemTypeNameHandling = TypeNameHandling.All)]
        public virtual object Value { get; protected set; }
    }
}
