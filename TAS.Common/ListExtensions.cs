using System.Collections.Generic;

namespace TAS.Common
{
    public static class ListExtensions
    {
        public static object SyncRoot<T>(this List<T> list)
        {
            return ((System.Collections.ICollection)list).SyncRoot;
        }
    }
}
