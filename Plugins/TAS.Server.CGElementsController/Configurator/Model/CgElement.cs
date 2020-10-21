using System.Drawing;
using TAS.Common.Interfaces;
using TAS.Database.Common;

namespace TAS.Server.CgElementsController.Configurator.Model
{    
    public class CgElement : ICGElement
    {
        [Hibernate]
        public byte Id { get; set; }
        [Hibernate]
        public string Name { get; set; }
        [Hibernate]
        public string Command { get; set; }
        [Hibernate]
        public Bitmap Image { get; }
    }
}
