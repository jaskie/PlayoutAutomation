using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TAS.Remoting
{
    [JsonObject(IsReference = false)]
    public class PropertyChangedWithValueEventArgs: PropertyChangedEventArgs
    {
        public PropertyChangedWithValueEventArgs(string propertyName, object value) : base(propertyName)
        {
            Value = value;
        }
        public object Value { get; private set; }
    }
}
