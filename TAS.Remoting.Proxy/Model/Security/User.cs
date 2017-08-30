using System.Collections.ObjectModel;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model.Security
{
    public class User: SecurityObjectBase, IUser
    {
#pragma warning disable CS0649 
        [JsonProperty(nameof(IUser.Name))]
        private string _name;
        
        [JsonProperty(nameof(IUser.IsAuthenticated))]
        private bool _isAuthenticated;

        [JsonProperty(nameof(IUser.AuthenticationType))]
        private string _authenticationType;

        [JsonProperty(nameof(IUser.IsAdmin))]
        private bool _isAdmin;

        [JsonProperty(nameof(IUser.AuthenticationSource))]
        private AuthenticationSource _authenticationSource;

        [JsonProperty(nameof(IUser.AuthenticationObject))]
        private string _authenticationObject;
#pragma warning restore

        public string Name { get { return _name; } set { Set(value); } }

        public string AuthenticationType { get { return _authenticationType; } set { Set(value); } }

        public bool IsAuthenticated { get { return _isAuthenticated; } set { Set(value); } }

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
        
        protected override void OnEventNotification(WebSocketMessage e)
        {
        }
    }
}
