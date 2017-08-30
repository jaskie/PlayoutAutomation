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
    }

    [DebuggerDisplay("{PropertyName} = {Value}")]
    public class PropertyChangedWithValueEventArgs : PropertyChangedWithDataEventArgs
    {
        public PropertyChangedWithValueEventArgs(string propertyName, object value) : base(propertyName)
        {
            Value = value;
        }

        [JsonProperty]
        public virtual object Value { get; private set; }
    }

    [DebuggerDisplay("{PropertyName} = {Value}")]
    public class PropertyChangedWithArrayEventArgs : PropertyChangedWithDataEventArgs
    {
        public PropertyChangedWithArrayEventArgs(string propertyName, object value) : base(propertyName)
        {
            Value = value;
        }

        [JsonProperty(ItemIsReference = true, TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.Objects)]
        public virtual object Value { get; }
    }
}
