using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Server.Interfaces;
using TAS.Server.Common;

namespace TAS.Server
{
    [DebuggerDisplay("{Id}:{Name}")]
    public class CGElement : Remoting.Server.DtoBase, ICGElement
    {
        [XmlAttribute]
        public byte Id { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
        private string _imageFile;
        [XmlAttribute]
        public string ImageFile
        {
            get { return _imageFile; }
            set
            {
                _imageFile = Path.Combine(FileUtils.CONFIGURATION_PATH, value);
            }
        }
    }
}
