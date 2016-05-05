using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Remoting.Server
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class DtoBase: IDto
    {
        private readonly Guid _guidDto = Guid.NewGuid();
        [JsonProperty]
        public Guid DtoGuid { get { return _guidDto; } }

    }
}
