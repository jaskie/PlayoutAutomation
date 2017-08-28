using System;
using TAS.Common.Interfaces;
using TAS.Remoting.Client;

namespace TAS.Remoting.Model.Security
{
    public class EventAclItem: ProxyBase, IAclRight
    {
        protected override void OnEventNotification(WebSocketMessage e)
        {
            throw new NotImplementedException();
        }

        public IPersistent Owner { get { return Get<Event>(); } set {SetLocalValue(value);} }
        public ISecurityObject SecurityObject { get; set; }
        public ulong Acl { get { return Get<ulong>(); } set {Set(value);} }
    }
}
