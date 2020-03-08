using System.Collections.Generic;

namespace AutoPriorities.Extensions
{
    public static class CollectionExtensions
    {
        public static IEnumerable<T> NonRepeating<T>(this IEnumerable<T> enumer, IEqualityComparer<T> comparer)
        {
            var hashSet = new HashSet<T>(comparer);
            foreach (var item in enumer)
            {
                if (!hashSet.Contains(item))
                {
                    yield return item;
                }

                hashSet.Add(item);
            }
        }

        public static IEnumerable<(int i, int interval)> IterPercents(this IEnumerable<float> percents, int iterations)
        {
            var iters = 0;
            var percentIter = 0;
            foreach (var percent in percents)
            {
                var toIter = iters + (int) (percent * iterations);
                for (var i = iters; i < toIter && i < iterations; i++)
                {
                    yield return (i, percentIter);
                    iters += 1;
                }

                percentIter += 1;
            }
        }
    }
}