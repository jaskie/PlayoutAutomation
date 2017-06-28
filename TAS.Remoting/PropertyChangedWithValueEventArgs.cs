using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;

namespace TAS.Remoting
{
    [JsonObject(IsReference = false)]
    [DebuggerDisplay("{PropertyName} = {Value}")]
    public class PropertyChangedWithValueEventArgs : PropertyChangedEventArgs
    {
        public PropertyChangedWithValueEventArgs(string propertyName, object value) : base(propertyName)
        {
            Value = value;
        }
        [JsonProperty]
        public object Value { get; private set; }
        public override string ToString()
        {
            return $"{PropertyName} = {Value}";
        }
    }
}
