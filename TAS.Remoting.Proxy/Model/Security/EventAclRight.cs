using System;
using Newtonsoft.Json;
using TAS.Common.Interfaces;
using TAS.Remoting.Client;

namespace TAS.Remoting.Model.Security
{
    public class EventAclRight: ProxyBase, IAclRight
    {
        [JsonProperty(nameof(IAclRight.Owner))]
        private Event _owner;

        [JsonProperty(nameof(IAclRight.SecurityObject))]
        private ISecurityObject _securityObject;

        [JsonProperty(nameof(IAclRight.Acl))]
        private ulong _acl;

        public IPersistent Owner { get { return _owner; } set {Set(value);} }

        public ISecurityObject SecurityObject { get { return _securityObject; } set {Set(value);} }

        public ulong Acl { get { return _acl; } set {Set(value);} }

        protected override void OnEventNotification(WebSocketMessage message)
        {

        }
    }
}
