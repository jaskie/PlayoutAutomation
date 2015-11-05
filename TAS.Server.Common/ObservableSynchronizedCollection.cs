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
            item.PropertyChanged += NotifyItemPropertyChanged;
            NotifyCollectionOperation(item, TCollectionOperation.Insert);
        }

        protected override void RemoveItem(int index)
        {
            lock (SyncRoot)
                if (Count > index)
                {
                    T item = this[index];
                    item.PropertyChanged -= NotifyItemPropertyChanged;
                    NotifyCollectionOperation(item, TCollectionOperation.Remove);
                }
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, T item)
        {
            lock (SyncRoot)
                if (Count > index)
                {
                    T olditem = this[index];
                    olditem.PropertyChanged -= NotifyItemPropertyChanged;
                    NotifyCollectionOperation(olditem, TCollectionOperation.Remove);
                    base.SetItem(index, item);
                    item.PropertyChanged += NotifyItemPropertyChanged;
                    NotifyCollectionOperation(item, TCollectionOperation.Insert);
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
            var handler = CollectionOperation;
            if (handler != null)
                handler(this, new CollectionOperationEventArgs<T>(item, operation));
        }

        public event EventHandler<PropertyChangedEventArgs> ItemPropertyChanged;
        private void NotifyItemPropertyChanged(object o, PropertyChangedEventArgs e)
        {
            var handler = ItemPropertyChanged;
            if (handler != null)
                handler(o, e);
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
