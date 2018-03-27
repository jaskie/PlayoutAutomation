using Newtonsoft.Json;
using TAS.Common.Interfaces;
using TAS.Remoting.Server;

namespace TAS.Server.Security
{
    public abstract class AclRightBase: DtoBase, IAclRight, IPersistent
    {
        private ulong _acl;
        private ISecurityObject _securityObject;
        public IPersistent Owner { get; set; }

        /// <summary>
        /// object to who right is assigned
        /// </summary>
        [JsonProperty]
        public ISecurityObject SecurityObject
        {
            get => _securityObject;
            set => SetField(ref _securityObject, value);
        }

        [JsonProperty]
        public ulong Acl
        {
            get => _acl;
            set => SetField(ref _acl, value);
        }

        public ulong Id { get; set; }

        public abstract void Save();

        public abstract void Delete();
    }
}
