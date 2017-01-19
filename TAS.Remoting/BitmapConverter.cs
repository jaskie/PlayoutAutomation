using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace TAS.Remoting
{
    public class BitmapConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Bitmap);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string s = reader.Value as string;
            if (!string.IsNullOrWhiteSpace(s))
            {
                MemoryStream ms = new MemoryStream(Convert.FromBase64String(s));
                return new Bitmap(ms);
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var bitmap = value as Bitmap;
            if (bitmap != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    writer.WriteValue(ms.ToArray());
                }                
            }
        }
    }
}
