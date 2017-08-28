using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Remoting.Client;

namespace TAS.Remoting.Model.Security
{
    public class User: SecurityObjectBase, IUser
    {
        public string Name { get { return Get<string>(); } set { Set(value); } }

        public string AuthenticationType { get { return Get<string>(); } set { Set(value); } }

        public bool IsAuthenticated { get { return Get<bool>(); } set { Set(value); } }

        [JsonProperty(nameof(Groups))]
        private ReadOnlyCollection<Group> _groups { get { return Get<ReadOnlyCollection<Group>>(); }  set { SetLocalValue(value);} }
        [JsonIgnore]
        public IReadOnlyCollection<IGroup> Groups => _groups;

        public void GroupAdd(IGroup group)
        {
            Invoke(parameters: new object[] {group});
        }

        public bool GroupRemove(IGroup group)
        {
            return Query<bool>(parameters: new object[] { group });
        }

        public bool IsAdmin { get { return Get<bool>(); } set { Set(value); } }

        public AuthenticationSource AuthenticationSource { get { return Get<AuthenticationSource>(); } set { Set(value);} }

        public string AuthenticationObject { get { return Get<string>(); } set { Set(value); } }
        
        protected override void OnEventNotification(WebSocketMessage e)
        {
            throw new NotImplementedException();
        }
    }
}
