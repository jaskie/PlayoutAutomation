using jNet.RPC;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Common.Interfaces.Security;

namespace TAS.Server.Security
{
    public class Group: SecurityObjectBase, IGroup
    {
        public Group():base(null) { }
        public Group(IAuthenticationService authenticationService): base(authenticationService) { }

        [DtoMember, XmlIgnore]
        public override SecurityObjectType SecurityObjectTypeType { get; } = SecurityObjectType.Group;

        public override void Save()
        {
            if (Id == default(ulong))
            {
                AuthenticationService.AddGroup(this);
                DatabaseProvider.Database.InsertSecurityObject(this);
            }
            else
                DatabaseProvider.Database.UpdateSecurityObject(this);
        }

        public override void Delete()
        {
            AuthenticationService.RemoveGroup(this);
            DatabaseProvider.Database.DeleteSecurityObject(this);
        }
    }
}
