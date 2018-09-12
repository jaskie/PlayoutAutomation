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
                return new PropertyChangedWithArrayEventArgs(propertyName) {Value = value};
            return new PropertyChangedWithValueEventArgs(propertyName) {Value = value};
        }

        public virtual object Value
        {
            get => (this as PropertyChangedWithValueEventArgs)?.Value ??
                   (this as PropertyChangedWithArrayEventArgs)?.Value;
            internal set => throw new System.NotImplementedException();
        }
    }

    [DebuggerDisplay("{PropertyName} = {Value}")]
    public class PropertyChangedWithValueEventArgs : PropertyChangedWithDataEventArgs
    {
        public PropertyChangedWithValueEventArgs(string propertyName) : base(propertyName) { }


        [JsonProperty]
        public override object Value { get; internal set; }
    }

    [DebuggerDisplay("{PropertyName} = {Value}")]
    public class PropertyChangedWithArrayEventArgs : PropertyChangedWithDataEventArgs
    {
        public PropertyChangedWithArrayEventArgs(string propertyName) : base(propertyName) { }

        [JsonProperty(ItemIsReference = true, TypeNameHandling = TypeNameHandling.All, ItemTypeNameHandling = TypeNameHandling.All)]
        public override object Value { get; internal set; }
    }
}
