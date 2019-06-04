using System;

namespace TAS.Common.Interfaces
{
    public class EventArgs<T> : EventArgs
    {
        public EventArgs(T item)
        {
            Item = item;
        }
        public T Item { get; }
    }
}