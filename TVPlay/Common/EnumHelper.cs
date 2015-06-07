using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using TAS.Common;

namespace TAS.Client
{
    public static class EnumHelper
    {
        public static string Description(this Enum eValue)
        {
            var nAttributes = eValue.GetType().GetField(eValue.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (!nAttributes.Any())
                return eValue.ToString();
            return (nAttributes.First() as DescriptionAttribute).Description;
        }

        public static System.Windows.Media.Color Color(this Enum eValue)
        {
            var nAttributes = eValue.GetType().GetField(eValue.ToString()).GetCustomAttributes(typeof(ColorAttribute), false);

            if (!nAttributes.Any())
                return System.Windows.Media.Colors.Transparent;
            return (nAttributes.First() as ColorAttribute).Color;
        }

        public static IEnumerable<ValueDescription> GetAllValuesAndDescriptions<TEnum>() where TEnum : struct, IConvertible, IComparable, IFormattable
        {
            if (!typeof(TEnum).IsEnum)
                throw new ArgumentException("TEnum must be an Enumeration type");

            return from e in Enum.GetValues(typeof(TEnum)).Cast<Enum>()
                   select new ValueDescription() { Value = e, Description = e.Description(), Color=e.Color() };
        }
        public static IEnumerable<ValueDescription> GetAllValuesAndDescriptionsAndNull<TEnum>() where TEnum : struct, IConvertible, IComparable, IFormattable
        {
            if (!typeof(TEnum).IsEnum)
                throw new ArgumentException("TEnum must be an Enumeration type");
            return (new List<ValueDescription>() {new ValueDescription() { Value = null, Description = "Wszystkie", Color=System.Windows.Media.Colors.Transparent }, }).Concat
                (from e in Enum.GetValues(typeof(TEnum)).Cast<Enum>()
                   select new ValueDescription() { Value = e, Description = e.Description(), Color=e.Color() });
        }
    }

    public class ValueDescription
    {
        public Enum Value {get; set;}
        public string Description {get; set;}
        public System.Windows.Media.Color Color { get; set; }
        public override string ToString()
        {
            return Description;
        }
    }
}
