using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace TAS.Server.Common
{
    public enum TDictionaryOperation {Add, Remove};
    public class SimpleDictionary<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, TValue> _dict;
        public SimpleDictionary()
        {
            _dict = new ConcurrentDictionary<TKey, TValue>();
        }
        public TValue this[TKey key]
        {
            get
            {
                TValue val;
                if (key != null && _dict.TryGetValue(key, out val))
                    return val;
                else
                    return default(TValue);
            }
            set
            {
                TValue val;
                if (value == null)
                    _dict.TryRemove(key, out val);
                else
                {
                    _dict.TryGetValue(key, out val);
                    _dict[key] = value;
                }
                if (val == null && value != null)
                    NotifyDictionaryOperation(key, value, TDictionaryOperation.Add);
                if (val != null && value == null)
                    NotifyDictionaryOperation(key, val, TDictionaryOperation.Remove);
                if (val != null && value != null)
                {
                    NotifyDictionaryOperation(key, val, TDictionaryOperation.Remove);
                    NotifyDictionaryOperation(key, value, TDictionaryOperation.Add);
                }
            }
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            if (_dict.TryRemove(key, out value))
            {
                NotifyDictionaryOperation(key, value, TDictionaryOperation.Remove);
                return true;
            }
            return false;
        }

        public ICollection<TKey> Keys { get { return _dict.Keys; } }
        public ICollection<TValue> Values { get { return _dict.Values; } }

        protected void Add(TKey key, TValue value)
        {
        }
   
        public void Clear()
        {
            foreach (TKey k in _dict.Keys.ToList())
                NotifyDictionaryOperation(k, this[k], TDictionaryOperation.Remove);
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dict.Contains(item);
        }

        public event EventHandler<DictionaryOperationEventArgs<TKey, TValue>> DictionaryOperation;
        private void NotifyDictionaryOperation(TKey key, TValue value, TDictionaryOperation operation)
        {
            var handler = DictionaryOperation;
            if (handler != null)
                handler(this, new DictionaryOperationEventArgs<TKey, TValue>(key, value, operation));
        }

   
    }

    public class DictionaryOperationEventArgs<TKey, TValue> : EventArgs
    {
        public DictionaryOperationEventArgs(TKey key, TValue value, TDictionaryOperation operation)
        {
            Operation  = operation;
            Key = key;
            Value = value;
        }
        public TDictionaryOperation Operation { get; private set; }
        public TKey Key { get; private set; }
        public TValue Value { get; private set; }
    }

}
