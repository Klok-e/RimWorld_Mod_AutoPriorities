using System.Collections.Generic;

namespace AutoPriorities.Extensions
{
    internal static class UsefulThings
    {
        public static IEnumerable<T> NonRepeating<T>(this IEnumerable<T> enumer, IEqualityComparer<T> comparer)
        {
            var hashSet = new HashSet<T>(comparer);
            foreach(var item in enumer)
            {
                if(!hashSet.Contains(item))
                {
                    yield return item;
                }
                hashSet.Add(item);
            }
        }
    }
}
