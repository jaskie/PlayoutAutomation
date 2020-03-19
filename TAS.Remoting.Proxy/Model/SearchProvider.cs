using System;
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

        public void Start()
        {
            Invoke();
        }

        public void Cancel()
        {
            Invoke();
        }

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

        protected override void OnEventNotification(SocketMessage message)
        {
            switch (message.MemberName)
            {
                case nameof(Finished):
                    _finished?.Invoke(this, EventArgs.Empty);
                    break;
                case nameof(ItemAdded):
                    _itemAdded?.Invoke(this, Deserialize<EventArgs<T>>(message));
                    break;
            }
        }
    }
}
