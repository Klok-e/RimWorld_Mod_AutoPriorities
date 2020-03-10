using System;
using System.Collections.Generic;

namespace AutoPriorities.Extensions
{
    public static class CollectionExtensions
    {
        public static IEnumerable<T> Distinct<T, K>(this IEnumerable<T> enumer, Func<T, K> key)
        {
            var hashSet = new HashSet<K>();
            foreach (var item in enumer)
            {
                if (!hashSet.Contains(key(item)))
                    yield return item;

                hashSet.Add(key(item));
            }
        }

        public static IEnumerable<(int i, int percentIndex)> IterPercents(this IEnumerable<double> percents,
            int iterations)
        {
            var iters = 0;
            var percentIter = 0;
            foreach (var percent in percents)
            {
                var toIter = iters + (int) Math.Ceiling(percent * iterations);
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