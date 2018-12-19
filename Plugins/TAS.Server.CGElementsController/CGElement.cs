using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using TAS.Common;
using Newtonsoft.Json;
using System.Drawing;
using TAS.Remoting;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    [DebuggerDisplay("{Id}:{Name}")]
    public class CGElement : Remoting.Server.DtoBase, ICGElement
    {
        private readonly object _imageLock = new object();
        
        [XmlAttribute]
        [JsonProperty]
        public byte Id { get; set; }
        [XmlAttribute]
        [JsonProperty]
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

        [JsonProperty]
        [JsonConverter(typeof(BitmapJsonConverter))]
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
