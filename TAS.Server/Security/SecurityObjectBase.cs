using System.Collections.Generic;
using System.Xml.Serialization;
using jNet.RPC;
using jNet.RPC.Server;
using TAS.Common;
using TAS.Common.Database;
using TAS.Common.Interfaces.Security;

namespace TAS.Server.Security
{
    /// <summary>
    /// base class for User and Group
    /// </summary>
    public abstract class SecurityObjectBase: ServerObjectBase, ISecurityObject
    {
        private string _name;

        protected SecurityObjectBase(IAuthenticationService authenticationService)
        {
            AuthenticationService = authenticationService;
            FieldLengths = DatabaseProvider.Database.SecurityObjectFieldLengths;
        }

        public abstract SecurityObjectType SecurityObjectTypeType { get; }

        [DtoMember, XmlIgnore]
        public IAuthenticationService AuthenticationService { get; set; }

        [XmlIgnore]
        public ulong Id { get; set; }

        [DtoMember, Hibernate]
        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public IDictionary<string, int> FieldLengths { get; }

        public abstract void Save();

        public abstract void Delete();

    }
}
