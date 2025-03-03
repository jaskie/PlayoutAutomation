using System.Collections.Generic;

namespace TAS.Common
{
    public static class ListExtensions
    {
        public static object SyncRoot<T>(this IList<T> list)
        {
            return ((System.Collections.ICollection)list).SyncRoot;
        }
    }
}
