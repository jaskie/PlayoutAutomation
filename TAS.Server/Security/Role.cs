using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Security
{
    public class Role: SecurityObjectBase, IRole
    {
        public const string Playout = nameof(Playout);
        public const string Media = nameof(Media);
        public const string Preview = nameof(Preview);
        public const string UserAdmin = nameof(UserAdmin);
        
        [XmlIgnore]
        public override SceurityObjectType SceurityObjectTypeType { get; } = SceurityObjectType.Role;
    }
}
