using System.Collections.Generic;
using System.Xml.Serialization;
using TAS.Common;

namespace TAS.Client.XKeys
{
    public class Backlight
    {

        [XmlAttribute]
        public TEngineState State { get; set; }

        [XmlAttribute]
        public BacklightColorEnum Color { get; set; }

        [XmlIgnore]
        public int[] Keys { get; private set; }

        [XmlAttribute(nameof(Keys))]
        public string KeysAsString
        {
            get => string.Join(",", Keys);
            set
            {
                if (value == null)
                    Keys = new int[0];
                else
                {
                    var split = value.Split(',', ';');
                    var list = new List<int>();
                    foreach (var s in split)
                        if (int.TryParse(s, out var key))
                            list.Add(key);
                    Keys = list.ToArray();
                }
            }
        }

        [XmlAttribute]
        public bool Blinking { get; set; }
    }
}
