using System;

namespace TAS.Remoting.Server
{
    internal class ReceivedDto : IDto
    {
        public Guid DtoGuid { get; set; }

        public void ReferenceAdd() { }

        public void ReferenceRemove() { }
    }
}
