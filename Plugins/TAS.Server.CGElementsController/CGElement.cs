using System.Diagnostics;
using System.IO;
using TAS.Common;
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
        private readonly object _imageLock = new object();
        
        [Hibernate]
        [DtoMember]
        public byte Id { get; set; }

        [Hibernate]
        [DtoMember]
        public string Name { get; set; }
        private string _imageFile;
        [Hibernate]
        public string ImageFile
        {
            get => _imageFile;
            set
            {
                _imageFile = Path.Combine(FileUtils.ConfigurationPath, value);
                if (!File.Exists(_imageFile)) return;
                lock(_imageLock)
                    _image = new Bitmap(_imageFile);
            }
        }

        private Bitmap _image;

        [DtoMember]
        public Bitmap Image
        {
            get
            {
                lock(_imageLock)
                    return _image?.Clone() as Bitmap;
            }
        }

        [Hibernate]
        public string Command { get; set; }

    }
}
