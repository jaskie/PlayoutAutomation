using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Remoting.Server
{
    [JsonObject(ItemTypeNameHandling = TypeNameHandling.Objects, IsReference = true, ItemIsReference = true, MemberSerialization = MemberSerialization.OptIn)]
    public abstract class DtoBase: IDto
    {
        public Guid DtoGuid { get; set; }
    }
}
