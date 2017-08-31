using System;
using System.Collections.Generic;
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
    /// <summary>
    /// base class for User and Group
    /// </summary>
    public abstract class SecurityObjectBase : ProxyBase, ISecurityObject
    {
        [JsonProperty(nameof(ISecurityObject.SecurityObjectTypeType))]
        private SecurityObjectType _securityObjectType;

        public SecurityObjectType SecurityObjectTypeType => _securityObjectType;

        public ulong Id { get; set; }

        public void Save()
        {
            Invoke();
        }

        public void Delete()
        {
            Invoke();
        }

        protected override void OnEventNotification(string memberName, EventArgs e)
        {
        }
    }
}
