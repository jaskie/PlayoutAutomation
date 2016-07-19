using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Client.Common
{
    public static class EnumerableExtensions
    {
        public static string AsString<TSource>(this IEnumerable<TSource> collection, string separator, int maxItems = 20)
        {
            StringBuilder sb = new StringBuilder(string.Join(separator, collection.Take(maxItems)));
            if (collection.Count() > maxItems)
            {
                sb.AppendLine("...")
                  .AppendFormat(TAS.Client.Common.Properties.Resources._moreItems, collection.Count() - maxItems);
            }
            return sb.ToString();
        }
    }
}
