using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;


namespace TAS.Server.Common
{
    public enum CollectionOperation { Insert, Remove };
    public class ObservableSynchronizedCollection<T> : SynchronizedCollection<T> where T: INotifyPropertyChanged
    {
        public ObservableSynchronizedCollection() { }
        public ObservableSynchronizedCollection(object syncRoot, IEnumerable<T> items) : base(syncRoot, items) { }

        public List<T> ToList()
        {
            lock (SyncRoot)
                return new List<T>(this);
        }

        public bool RemoveWhere(Func<T, bool> predicate)
        {
            lock (SyncRoot)
            {
                List<T> itemsToRemove = this.Where(predicate).ToList();
                if (itemsToRemove.Count > 0)
                    return itemsToRemove.All(i => Remove(i));
                else
                    return false;
            }
        }

        public event EventHandler<CollectionOperationEventArgs<T>> CollectionOperation;

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            NotifyCollectionOperation(item, Common.CollectionOperation.Insert);
        }

        protected override void RemoveItem(int index)
        {
            T item = default(T);
            lock (SyncRoot)
                if (Count > index)
                {
                    item = this[index];
                }
            base.RemoveItem(index);
            if (item != null)
                NotifyCollectionOperation(item, Common.CollectionOperation.Remove);
        }
       
        protected override void SetItem(int index, T item)
        {
            lock (SyncRoot)
                if (Count > index)
                    base.SetItem(index, item);
        }

        protected override void ClearItems()
        {
            lock (SyncRoot)
                while (Count>0) 
                    RemoveItem(0);
        }

        private void NotifyCollectionOperation(T item, CollectionOperation operation)
        {
            CollectionOperation?.Invoke(this, new CollectionOperationEventArgs<T>(item, operation));
        }

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
