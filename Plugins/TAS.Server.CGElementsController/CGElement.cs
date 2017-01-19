using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using Newtonsoft.Json;
using System.Drawing;

namespace TAS.Server
{
    [DebuggerDisplay("{Id}:{Name}")]
    public class CGElement : Remoting.Server.DtoBase, ICGElement
    {
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
            get { return _imageFile; }
            set
            {
                _imageFile = Path.Combine(FileUtils.CONFIGURATION_PATH, value);
                if (File.Exists(_imageFile))
                    _image = new Bitmap(_imageFile);
            }
        }

        private Bitmap _image;
        public Bitmap Image
        {
            get
            {
                return _image;
            }
        }
    }
}
