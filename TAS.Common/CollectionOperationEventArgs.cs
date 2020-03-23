using System;

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

        public CollectionOperation Operation { get; }
        
        public T Item { get; }
    }
}
