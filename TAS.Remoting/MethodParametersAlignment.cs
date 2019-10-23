using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TAS.Remoting
{
    internal static class MethodParametersAlignment
    {
        public static T AlignType<T>(this JsonSerializer serializer, object input)
        {
            if (input == null)
                return default(T);
            if (input is JArray)
                using (var reader = new StringReader(input.ToString()))
                {
                    return (T)serializer.Deserialize(reader, typeof(T));
                }
            AlignType(ref input, typeof(T));
            return (T)input;
        }

        public static void AlignType(ref object input, Type type)
        {
            if (type.IsEnum)
                input = Enum.Parse(type, input.ToString());
            else
            if (input is string && type == typeof(TimeSpan))
                input = TimeSpan.Parse((string)input, System.Globalization.CultureInfo.InvariantCulture);
            else
            if (input is string && type == typeof(TimeSpan?))
                input = TimeSpan.Parse((string)input, System.Globalization.CultureInfo.InvariantCulture);
            else
            if (input is string && type == typeof(Guid))
                input = Guid.Parse((string)input);
            else
            if (type.IsValueType && input != null && !type.IsGenericType)
                input = Convert.ChangeType(input, type);
        }
    }
}
