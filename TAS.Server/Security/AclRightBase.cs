using System;
using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Server;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Security;

namespace TAS.Server.Security
{
    public abstract class AclRightBase: ServerObjectBase, IAclRight, IPersistent
    {
        private ulong _acl;
        private ISecurityObject _securityObject;
        public IPersistent Owner { get; set; }

        /// <summary>
        /// object to who right is assigned
        /// </summary>
        [DtoField]
        public ISecurityObject SecurityObject
        {
            get => _securityObject;
            set => SetField(ref _securityObject, value);
        }

        [DtoField]
        public ulong Acl
        {
            get => _acl;
            set => SetField(ref _acl, value);
        }

        public ulong Id { get; set; }

        public IDictionary<string, int> FieldLengths { get; } = new Dictionary<string, int>();

        public event EventHandler Saved;

        public virtual void Save()
        {
            Saved?.Invoke(this, EventArgs.Empty);
        }

        public abstract void Delete();
    }
}
