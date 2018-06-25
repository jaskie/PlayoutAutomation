using Newtonsoft.Json;
using TAS.Common.Interfaces;
using TAS.Remoting.Client;

namespace TAS.Remoting.Model.Security
{
    public class EngineAclRight: ProxyBase, IAclRight
    {
#pragma warning disable CS0649
        [JsonProperty(nameof(IAclRight.Owner))]
        private Event _owner;

        [JsonProperty(nameof(IAclRight.SecurityObject))]
        private ISecurityObject _securityObject;

        [JsonProperty(nameof(IAclRight.Acl))]
        private ulong _acl;
#pragma warning restore

        public IPersistent Owner { get => _owner; set => Set(value); }

        public ISecurityObject SecurityObject { get => _securityObject; set => Set(value); }

        public ulong Acl { get => _acl; set => Set(value); }

        protected override void OnEventNotification(WebSocketMessage message)
        {

        }
    }
}
