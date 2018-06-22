using System;
using System.Collections.Generic;
using TAS.Client.Common;

namespace TAS.Client.ViewModels
{
    public class KeyValueEditViewmodel : ViewModelBase
    {

        private string _key;
        private string _value;
        
        public KeyValueEditViewmodel(KeyValuePair<string, string> item, bool keyIsEnabled)
        {
            _key = item.Key;
            _value = item.Value;
            KeyIsEnabled = keyIsEnabled;
        }

        public string Key
        {
            get => _key;
            set => SetField(ref _key, value);
        }

        public bool KeyIsEnabled { get; }

        public string Value
        {
            get => _value;
            set => SetField(ref _value, value);
        }

        public KeyValuePair<string, string> Result => new KeyValuePair<string, string>(Key, Value);

        protected override void OnDispose()
        {
        }
    }
}
