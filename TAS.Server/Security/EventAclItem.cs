using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Database;
using TAS.Common.Interfaces;
using TAS.Remoting.Server;

namespace TAS.Server.Security
{
    public class EventAclItem: DtoBase, IAclRight, IPersistent
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
            get { return _securityObject; }
            set { SetField(ref _securityObject, value); }
        }

        [JsonProperty]
        public ulong Acl
        {
            get { return _acl; }
            set { SetField(ref _acl, value); }
        }

        public ulong Id { get; set; }

        public void Save()
        {
            if (Id == default(ulong))
                this.DbInsertEventAcl();
            else
                this.DbUpdateEventAcl();
        }

        public void Delete()
        {
            this.DbDeleteEventAcl();
        }
    }
}
