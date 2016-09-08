using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
                _image = new BitmapImage();
                // BitmapImage.UriSource must be in a BeginInit/EndInit block.
                if (File.Exists(_imageFile))
                {
                    _image.BeginInit();
                    _image.UriSource = new Uri(_imageFile, UriKind.Relative);
                    _image.CacheOption = BitmapCacheOption.OnLoad;
                    _image.EndInit();
                    _image.Freeze();
                }
            }
        }

        private BitmapImage _image;
        public BitmapImage Image
        {
            get
            {
                return _image;
            }
        }
    }
}
