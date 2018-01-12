using System.Xml.Serialization;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Security
{
    public class Group: SecurityObjectBase, IGroup
    {
        public Group():base(null) { }
        public Group(IAuthenticationService authenticationService): base(authenticationService) { }

        [JsonProperty, XmlIgnore]
        public override SecurityObjectType SecurityObjectTypeType { get; } = SecurityObjectType.Group;

        public override void Save()
        {
            if (Id == default(ulong))
            {
                AuthenticationService.AddGroup(this);
                EngineController.Database.DbInsertSecurityObject(this);
            }
            else
                EngineController.Database.DbUpdateSecurityObject(this);
        }

        public override void Delete()
        {
            AuthenticationService.RemoveGroup(this);
            EngineController.Database.DbDeleteSecurityObject(this);
            Dispose();
        }
    }
}
