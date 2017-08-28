using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Database;
using TAS.Common.Interfaces;

namespace TAS.Server.Security
{
    public class EventAclItem: IAclRight, IPersistent
    {
        public IPersistent Owner { get; set; }
        /// <summary>
        /// object to who right is assigned
        /// </summary>
        public ISecurityObject SecurityObject { get; set; }
        public ulong Acl { get; set; }
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
