﻿using System;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public abstract class SearchProvider<T> : ProxyObjectBase, ISearchProvider<T>
    {

        private event EventHandler _finished;
        private event EventHandler<EventArgs<T>> _itemAdded;

        public void Start() => Invoke();

        public void Cancel() => Invoke();

        public event EventHandler<EventArgs<T>> ItemAdded
        {
            add
            {
                EventAdd(_itemAdded);
                _itemAdded += value;
            }
            remove
            {
                _itemAdded -= value;
                EventRemove(_itemAdded);
            }
        }

        public event EventHandler Finished
        {
            add
            {
                EventAdd(_finished);
                _finished += value;
            }
            remove
            {
                _finished -= value;
                EventRemove(_finished);
            }
        }

        protected override void OnEventNotification(string eventName, EventArgs eventArgs)
        {
            switch (eventName)
            {
                case nameof(Finished):
                    _finished?.Invoke(this, eventArgs);
                    return;
                case nameof(ItemAdded):
                    _itemAdded?.Invoke(this, (EventArgs<T>)eventArgs);
                    return;
            }
            base.OnEventNotification(eventName, eventArgs);
        }
    }
}
