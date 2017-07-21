using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Server.Common;
using TAS.Server.Common.Database;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Security
{
    public class AcoHive<TItem> where TItem : ISecurityObject
    {
        private readonly List<TItem> _items;

        internal AcoHive(IEnumerable<TItem> items)
        {
            _items = new List<TItem>(items);
        }

        public IList<TItem> Items
        {
            get
            {
                lock (((IList)_items).SyncRoot)
                    return _items.ToList();
            }
        }

        public bool Add(TItem item)
        {
            bool result = false;
            lock (((IList) _items).SyncRoot)
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
            lock (((IList)_items).SyncRoot)
                isRemoved = _items.Remove(item);
            if (isRemoved)
            {
                item.DbDelete();
                AcoOperartion?.Invoke(this, new CollectionOperationEventArgs<TItem>(item, CollectionOperation.Remove));
            }
            return isRemoved;
        }

        public event EventHandler<CollectionOperationEventArgs<TItem>> AcoOperartion;
    }
}
