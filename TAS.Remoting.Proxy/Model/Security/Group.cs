using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Remoting.Client;
using TAS.Remoting.Server;

namespace TAS.Remoting.Model.Security
{
    public class Group: SecurityObjectBase, IGroup
    {
        [JsonProperty(nameof(IGroup.Name))]
        private string _name;

        public string Name { get { return _name; } set { Set(value); } }

        protected override void OnEventNotification(WebSocketMessage e)
        {
        }
    }
}
