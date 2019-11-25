using Newtonsoft.Json;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model.Security
{
    public class Group: SecurityObjectBase, IGroup
    {

#pragma warning disable CS0649
        [JsonProperty(nameof(IGroup.Name))]
        private string _name;
#pragma warning restore

        public string Name { get => _name; set => Set(value); }

    }
}
