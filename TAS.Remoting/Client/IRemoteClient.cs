using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace TAS.Remoting.Client
{
    public interface IRemoteClient
    {
        T Deserialize<T>(WebSocketMessage message);
        event EventHandler<WebSocketMessageEventArgs> EventNotification;
        T Query<T>(ProxyBase dto, string methodName = "", params object[] parameters);
        void Invoke(ProxyBase dto, string methodName = "", params object[] parameters);
        T Get<T>(ProxyBase dto, string propertyName = "");
        void Set(ProxyBase dto, object value, string propertyName = "");
        void EventAdd(ProxyBase dto, string eventName = "");
        void EventRemove(ProxyBase dto, string eventName = "");
        void ObjectRemove(ProxyBase dto);
    }
}
