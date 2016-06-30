using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;


namespace TAS.Server.Common
{
    public enum TCollectionOperation { Insert, Remove };
    public class ObservableSynchronizedCollection<T> : SynchronizedCollection<T> where T: INotifyPropertyChanged
    {
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            NotifyCollectionOperation(item, TCollectionOperation.Insert);
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
                NotifyCollectionOperation(item, TCollectionOperation.Remove);
        }

       
        protected override void SetItem(int index, T item)
        {
            lock (SyncRoot)
                if (Count > index)
                {
                    T olditem = this[index];
                    base.SetItem(index, item);
                }
        }

        protected override void ClearItems()
        {
            lock (SyncRoot)
                while (Count>0) 
                    RemoveItem(0);
        }

        public event EventHandler<CollectionOperationEventArgs<T>> CollectionOperation;
        private void NotifyCollectionOperation(T item, TCollectionOperation operation)
        {
            CollectionOperation?.Invoke(this, new CollectionOperationEventArgs<T>(item, operation));
        }

        public List<T> ToList()
        {
            lock(SyncRoot)
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

    }

    public class CollectionOperationEventArgs<T> : EventArgs
    {
        public CollectionOperationEventArgs(T item, TCollectionOperation operation)
        {
            Operation = operation;
            Item = item;
        }
        public TCollectionOperation Operation { get; private set; }
        public T Item { get; private set; }
    }

}
