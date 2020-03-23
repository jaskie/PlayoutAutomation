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

        [DtoField, XmlIgnore]
        public override SecurityObjectType SecurityObjectTypeType { get; } = SecurityObjectType.Group;

        public override void Save()
        {
            if (Id == default(ulong))
            {
                AuthenticationService.AddGroup(this);
                EngineController.Current.Database.InsertSecurityObject(this);
            }
            else
                EngineController.Current.Database.UpdateSecurityObject(this);
        }

        public override void Delete()
        {
            AuthenticationService.RemoveGroup(this);
            EngineController.Current.Database.DeleteSecurityObject(this);
            Dispose();
        }
    }
}
