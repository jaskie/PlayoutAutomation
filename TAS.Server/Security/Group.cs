using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TAS.Server.Common;
using TAS.Server.Common.Database;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Security
{
    public class Group: SecurityObjectBase, IGroup
    {
        public Group():base(null) { }
        public Group(IAuthenticationService authenticationService): base(authenticationService) { }

        [XmlIgnore]
        public override SceurityObjectType SceurityObjectTypeType { get; } = SceurityObjectType.Group;

        public override void Save()
        {
            if (Id == default(ulong))
            {
                AuthenticationService.AddGroup(this);
                this.DbInsert();
            }
            else
                this.DbUpdate();
        }

        public override void Delete()
        {
            AuthenticationService.RemoveGroup(this);
            this.DbDelete();
        }
    }
}
