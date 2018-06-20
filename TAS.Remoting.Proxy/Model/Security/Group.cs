using Newtonsoft.Json;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model.Security
{
    public class Group: SecurityObjectBase, IGroup
    {
        [JsonProperty(nameof(IGroup.Name))]
        private string _name;

        public string Name { get => _name; set => Set(value); }

    }
}
