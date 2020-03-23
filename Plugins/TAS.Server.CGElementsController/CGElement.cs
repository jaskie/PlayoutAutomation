using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using TAS.Common;
using System.Drawing;
using jNet.RPC;
using jNet.RPC.Server;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    [DebuggerDisplay("{Id}:{Name}")]
    public class CGElement : ServerObjectBase, ICGElement
    {
        private readonly object _imageLock = new object();
        
        [XmlAttribute]
        [DtoMember]
        public byte Id { get; set; }
        [XmlAttribute]
        [DtoMember]
        public string Name { get; set; }
        private string _imageFile;
        [XmlAttribute]
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

        [XmlAttribute]
        public string Command { get; set; }

    }
}
