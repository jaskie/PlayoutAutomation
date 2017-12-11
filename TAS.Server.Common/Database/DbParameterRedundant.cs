using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Database
{
    public class DbParameterRedundant
    {
        private readonly KeyValuePair<string, object> _parameter;
        public DbParameterRedundant(string key, object value)
        {
            _parameter = new KeyValuePair<string, object>(key, value);
        }
        public KeyValuePair<string, object> Parameter { get { return _parameter; } }
        public string Key { get { return _parameter.Key; } }
        public object Value { get { return _parameter.Value; } }
    }
}
