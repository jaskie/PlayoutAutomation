using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;

namespace TAS.Remoting
{
    public class BitmapJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Bitmap);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var s = reader.Value as string;
            if (string.IsNullOrWhiteSpace(s))
                return null;
            var ms = new MemoryStream(Convert.FromBase64String(s));
            return new Bitmap(ms);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is Bitmap bitmap))
                return;
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                writer.WriteValue(ms.ToArray());
            }
        }
    }
}
