using System.Collections.Generic;
using TAS.Client.Common;

namespace TAS.Client.ViewModels
{
    public class KeyValueEditViewmodel : ViewmodelBase
    {
        private class KeyValueData
        {
            public string Key;
            public string Value;
        }

        private readonly KeyValueData _keyData = new KeyValueData();
        private readonly bool _keyIsReadOnly;

        public KeyValueEditViewmodel(KeyValuePair<string, string> item, bool keyIsReadOnly)
        {
            _keyData.Key = item.Key;
            _keyData.Value = item.Value;
            _keyIsReadOnly = keyIsReadOnly;
        }

        public string Key
        {
            get { return _keyData.Key; }
            set { SetField(ref _keyData.Value, value); }
        }

        public bool KeyIsEnabled => !_keyIsReadOnly;

        public string Value { get { return _keyData.Value; } set { SetField(ref _keyData.Value, value); } }

        public KeyValuePair<string, string> Result => new KeyValuePair<string, string>(Key, Value);

        protected override void OnDispose()
        {
        }
    }
}
