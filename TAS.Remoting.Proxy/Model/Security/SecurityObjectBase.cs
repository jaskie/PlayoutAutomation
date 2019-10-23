using System.Collections.Generic;
using ComponentModelRPC;
using ComponentModelRPC.Client;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces.Security;

namespace TAS.Remoting.Model.Security
{
    /// <summary>
    /// base class for User and Group
    /// </summary>
    public abstract class SecurityObjectBase : ProxyBase, ISecurityObject
    {
        
#pragma warning disable CS0649
        [JsonProperty(nameof(ISecurityObject.SecurityObjectTypeType))]
        private SecurityObjectType _securityObjectType;

        [JsonProperty(nameof(ISecurityObject.FieldLengths))]
        private IDictionary<string, int> _fieldLengths;
#pragma warning restore

        public SecurityObjectType SecurityObjectTypeType => _securityObjectType;

        public ulong Id { get; set; }

        public IDictionary<string, int> FieldLengths { get => _fieldLengths; set => Set(value); }

        public void Save()
        {
            Invoke();
        }

        public void Delete()
        {
            Invoke();
        }

        protected override void OnEventNotification(SocketMessage message)
        {
        }
    }
}
