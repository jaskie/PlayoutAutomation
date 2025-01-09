using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces.Security;

namespace TAS.Server.Security
{
    public class AcoHive<TItem> where TItem : ISecurityObject

    {
        private readonly List<TItem> _items;

        internal AcoHive(IEnumerable<TItem> items)
        {
            _items = new List<TItem>(items);
        }

        public IReadOnlyCollection<TItem> Items
        {
            get
            {
                lock (_items.SyncRoot())
                    return _items.ToArray();
            }
        }

        public bool Add(TItem item)
        {
            bool result = false;
            lock (_items.SyncRoot())
            {
                if (!_items.Contains(item))
                {
                    _items.Add(item);
                    result = true;
                }
            }
            if (result)
                AcoOperartion?.Invoke(this, new CollectionOperationEventArgs<TItem>(item, CollectionOperation.Add));
            return result;
        }

        public bool Remove(TItem item)
        {
            bool isRemoved;
            lock (_items.SyncRoot())
                isRemoved = _items.Remove(item);
            if (isRemoved)
            {
                DatabaseProvider.Database.DeleteSecurityObject(item); ;
                AcoOperartion?.Invoke(this, new CollectionOperationEventArgs<TItem>(item, CollectionOperation.Remove));
            }
            return isRemoved;
        }

        public TItem Find(Predicate<TItem> match)
        {
            lock (_items.SyncRoot())
                return _items.Find(match);
        }

        public event EventHandler<CollectionOperationEventArgs<TItem>> AcoOperartion;
    }
}
