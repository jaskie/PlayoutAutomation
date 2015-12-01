using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Server.Remoting
{
    internal class ReceivedDto : IDto
    {
        public Guid DtoGuid { get; set; }

        public void Dispose() { }
    }
}
