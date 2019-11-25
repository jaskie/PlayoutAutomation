using System;
using Newtonsoft.Json;

namespace TAS.Common
{
    public enum CollectionOperation
    {
        Add,
        Remove
    }

    public class CollectionOperationEventArgs<T> : EventArgs
    {
        public CollectionOperationEventArgs(T item, CollectionOperation operation)
        {
            Operation = operation;
            Item = item;
        }

        [JsonProperty]
        public CollectionOperation Operation { get; private set; }
        [JsonProperty]
        public T Item { get; private set; }
    }
}
