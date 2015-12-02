using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Server.Remoting
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class DtoBase: IDto
    {
        private readonly Guid _guidDto = Guid.NewGuid();
        [JsonProperty]
        public Guid DtoGuid { get { return _guidDto; } }

        public void ReferenceAdd() { }

        public void ReferenceRemove() { }
    }
}
