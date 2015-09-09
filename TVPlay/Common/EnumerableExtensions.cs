using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Client.ViewModels
{
    public static class EnumerableExtensions
    {
        public static string AsString<TSource>(this IEnumerable<TSource> collection, string separator, int maxItems)
        {
            StringBuilder sb = new StringBuilder(string.Join(separator, collection.Take(maxItems)));
            if (collection.Count() > maxItems)
            {
                sb.AppendLine("...")
                  .AppendFormat(Properties.Resources._moreItems, collection.Count() - maxItems);
            }
            return sb.ToString();
        }
    }
}
