using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Database;
using TAS.Common.Interfaces;

namespace TAS.Server.Security
{
    public class Group: SecurityObjectBase, IGroup
    {
        public Group():base(null) { }
        public Group(IAuthenticationService authenticationService): base(authenticationService) { }

        [XmlIgnore]
        public override SecurityObjectType SecurityObjectTypeType { get; } = SecurityObjectType.Group;

        public override void Save()
        {
            if (Id == default(ulong))
            {
                AuthenticationService.AddGroup(this);
                this.DbInsertSecurityObject();
            }
            else
                this.DbUpdateSecurityObject();
        }

        public override void Delete()
        {
            AuthenticationService.RemoveGroup(this);
            this.DbDeleteSecurityObject();
        }
    }
}
