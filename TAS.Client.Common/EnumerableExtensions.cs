using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Client.Common
{
    public static class EnumerableExtensions
    {
        public static string AsString<TSource>(this IEnumerable<TSource> collection, string separator, int maxItems = 20)
        {
            var listCopy = new List<TSource>(collection);
            var sb = new StringBuilder(string.Join(separator, listCopy.Take(maxItems)));
            if (listCopy.Count > maxItems)
            {
                sb.AppendLine("...")
                  .AppendFormat(Properties.Resources._moreItems, listCopy.Count - maxItems);
            }
            return sb.ToString();
        }
    }
}
