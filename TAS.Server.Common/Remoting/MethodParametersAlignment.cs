using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TAS.Server.Remoting
{
    public static class MethodParametersAlignment
    {
        /// <summary>
        /// converts method parameters to reqiuired types
        /// </summary>
        /// <param name="input"></param>
        /// <param name="parameters"></param>
        public static void AlignParameters(ref object[] input, params ParameterInfo[] parameters)
        {
            if (input.Length != parameters.Length)
                throw new ArgumentException(string.Format("{0}:{1} {2}", MethodInfo.GetCurrentMethod(), "Invalid number of arguments"));
            for (int i = 0; i < input.Length; i++)
            {
                Type parameterType = parameters[i].ParameterType;
                IEnumerable e = input[i] as IEnumerable;
                if (e != null)
                {
                    Type[] genericArgumentTypes = parameterType.GetGenericArguments();
                    if (genericArgumentTypes.Length == 1)
                    {
                        Type listType = typeof(List<>);
                        IList list = (IList)Activator.CreateInstance(listType.MakeGenericType(genericArgumentTypes));
                        foreach (object o in e)
                            list.Add(o);
                        input[i] = list;
                    }
                }
                AlignType(ref input[i], parameters[i].ParameterType);
            }
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
