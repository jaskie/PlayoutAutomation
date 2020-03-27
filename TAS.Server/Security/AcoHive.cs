using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public ReadOnlyCollection<TItem> Items
    {
        get
        {
            lock (((IList) _items).SyncRoot)
                return _items.AsReadOnly();
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
        lock (((IList) _items).SyncRoot)
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
        lock (((IList) _items).SyncRoot)
            return _items.Find(match);
    }

    public event EventHandler<CollectionOperationEventArgs<TItem>> AcoOperartion;
    }
}
