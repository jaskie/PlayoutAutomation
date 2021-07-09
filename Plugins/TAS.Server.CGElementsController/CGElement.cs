using System.Diagnostics;
using System.Drawing;
using jNet.RPC;
using jNet.RPC.Server;
using TAS.Common.Interfaces;
using TAS.Database.Common;

namespace TAS.Server.CgElementsController
{
    [DebuggerDisplay("{Id}:{Name}")]
    public class CGElement : ServerObjectBase, ICGElement
    {
        [DtoMember, Hibernate]
        public byte Id { get; set; }

        [DtoMember, Hibernate]
        public string Name { get; set; }

        [DtoMember, Hibernate]
        public Bitmap Thumbnail { get; set; }

        [Hibernate]
        public string Command { get; set; }

    }
}
