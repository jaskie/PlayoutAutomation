using jNet.RPC;
using jNet.RPC.Client;
using System;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Security;

namespace TAS.Remoting.Model.Security
{
    public class EventAclRight: ProxyObjectBase, IAclRight
    {
#pragma warning disable CS0649
        [DtoMember(nameof(IAclRight.Owner))]
        private Event _owner;

        [DtoMember(nameof(IAclRight.SecurityObject))]
        private ISecurityObject _securityObject;

        [DtoMember(nameof(IAclRight.Acl))]
        private ulong _acl;
#pragma warning restore

        public IPersistent Owner { get => _owner; set => Set(value); }

        public ISecurityObject SecurityObject { get => _securityObject; set => Set(value); }

        public ulong Acl { get => _acl; set => Set(value); }

        protected override void OnEventNotification(string eventName, EventArgs eventArgs)
        {

        }
    }
}
