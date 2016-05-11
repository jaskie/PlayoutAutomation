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
            AlignType(ref input, typeof(T));
            return (T)input;
        }

        public static void AlignType(ref object input, Type type)
        {
            if (type.IsEnum)
                input = Enum.Parse(type, input.ToString());
            else
            if (type == typeof(TimeSpan))
                input = TimeSpan.Parse((string)input, System.Globalization.CultureInfo.InvariantCulture);
            else
            if (type.IsValueType)
                input = Convert.ChangeType(input, type);
        }
    }
}
