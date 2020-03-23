using jNet.RPC;
using System.Collections.ObjectModel;
using TAS.Common;
using TAS.Common.Interfaces.Security;

namespace TAS.Remoting.Model.Security
{
    public class User: SecurityObjectBase, IUser
    {
#pragma warning disable CS0649 
        [DtoField(nameof(IUser.Name))]
        private string _name;
        
        [DtoField(nameof(IUser.IsAuthenticated))]
        private bool _isAuthenticated;

        [DtoField(nameof(IUser.AuthenticationType))]
        private string _authenticationType;

        [DtoField(nameof(IUser.IsAdmin))]
        private bool _isAdmin;

        [DtoField(nameof(IUser.AuthenticationSource))]
        private AuthenticationSource _authenticationSource;

        [DtoField(nameof(IUser.AuthenticationObject))]
        private string _authenticationObject;
#pragma warning restore

        public string Name { get => _name; set => Set(value); }

        public string AuthenticationType { get => _authenticationType; set => Set(value); }

        public bool IsAuthenticated { get => _isAuthenticated; set => Set(value); }

        public ReadOnlyCollection<IGroup> GetGroups()
        {
            return Query<ReadOnlyCollection<IGroup>>();
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
