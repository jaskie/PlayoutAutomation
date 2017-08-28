using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Remoting.Client;
using TAS.Remoting.Server;

namespace TAS.Remoting.Model.Security
{
    public class Group: SecurityObjectBase, IGroup
    {

        public string Name { get { return Get<string>(); } set { Set(value); } }

        protected override void OnEventNotification(WebSocketMessage e)
        {
            throw new NotImplementedException();
        }
    }
}
