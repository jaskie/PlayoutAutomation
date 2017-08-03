using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Server.Common;
using TAS.Server.Common.Database;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Security
{
    public class EventAclItem: IAclItem, IPersistent
    {
        public IPersistent Owner { get; set; }
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
