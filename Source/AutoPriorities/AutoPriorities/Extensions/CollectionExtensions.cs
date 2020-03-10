using System;
using System.Collections.Generic;
using System.Linq;

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

        public static IEnumerable<double> Cumulative(this IEnumerable<double> enu)
        {
            double cum = 0;
            foreach (var v in enu)
            {
                cum += v;
                yield return cum;
            }
        }

        public static IEnumerable<(int i, int percentIndex)> IterPercents(this IEnumerable<double> percents,
            int total)
        {
            var iter = 0;
            var toIter = 0d;
            var percentInd = 0;
            foreach (var percent in percents)
            {
                toIter += percent * total;
                for (; iter < toIter && iter < total; iter++)
                    yield return (iter, percentInd);

                percentInd += 1;
            }
        }
    }
}