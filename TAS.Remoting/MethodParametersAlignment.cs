using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TAS.Remoting
{
    public static class MethodParametersAlignment
    {
        public static T AlignType<T>(object input)
        {
            if (input == null)
                return default(T);
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
            if (type.IsValueType && input != null)
                input = Convert.ChangeType(input, type);
        }
    }
}
