using jNet.RPC;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces.Security;

namespace TAS.Remoting.Model.Security
{
    public class User: SecurityObjectBase, IUser
    {
#pragma warning disable CS0649 
        [DtoMember(nameof(IUser.Name))]
        private string _name;
        
        [DtoMember(nameof(IUser.IsAuthenticated))]
        private bool _isAuthenticated;

        [DtoMember(nameof(IUser.AuthenticationType))]
        private string _authenticationType;

        [DtoMember(nameof(IUser.IsAdmin))]
        private bool _isAdmin;

        [DtoMember(nameof(IUser.AuthenticationSource))]
        private AuthenticationSource _authenticationSource;

        [DtoMember(nameof(IUser.AuthenticationObject))]
        private string _authenticationObject;
#pragma warning restore

        public string Name { get => _name; set => Set(value); }

        public string AuthenticationType { get => _authenticationType; set => Set(value); }

        public bool IsAuthenticated { get => _isAuthenticated; set => Set(value); }

        public IReadOnlyCollection<IGroup> GetGroups()
        {
            return Query<IGroup[]>();
        }

        public void GroupAdd(IGroup group)
        {
            Invoke(parameters: new object[] {group});
        }

        public bool GroupRemove(IGroup group)
        {
            return Query<bool>(parameters: new object[] { group });
        }

        public bool IsAdmin { get { return _isAdmin; } set { Set(value); } }

        public AuthenticationSource AuthenticationSource { get { return _authenticationSource; } set { Set(value);} }

        public string AuthenticationObject { get { return _authenticationObject; } set { Set(value); } }
        
    }
}
