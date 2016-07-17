using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Client.ViewModels
{
    public class TupleEditViewmodel<TKey> : Common.OkCancelViewmodelBase<Tuple<TKey, string>>
    {
        private readonly bool _keyIsReadOnly;
        private TKey _key;
        private string _value;

        public TupleEditViewmodel(TKey key, string value, bool keyIsReadOnly): base(new Tuple<TKey, string>(key, value), new Views.TupleEditView(), key.ToString())
        {
            _key = key;
            _value = value;
            _keyIsReadOnly = keyIsReadOnly;
        }

        public TKey Key
        {
            get { return _key; }
            set
            {
                if (SetField(ref _key, value, "Key"))
                    Title = _key.ToString();
            }
        }

        public bool KeyIsEnabled { get { return !_keyIsReadOnly; } }

        public string Value { get { return _value; } set { SetField(ref _value, value, "Value"); } }

        public Tuple<TKey, string> Result { get { return new Tuple<TKey, string>(Key, Value); } }

        protected override void OnDispose()
        {
            
        }
    }
}
