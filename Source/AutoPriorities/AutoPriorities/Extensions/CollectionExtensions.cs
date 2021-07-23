using System;
using System.Collections.Generic;

namespace AutoPriorities.Extensions
{
    public static class CollectionExtensions
    {
        public static IEnumerable<T> Distinct<T, TK>(this IEnumerable<T> enumer, Func<T, TK> key)
        {
            var hashSet = new HashSet<TK>();
            foreach (var item in enumer)
            {
                if (!hashSet.Contains(key(item))) yield return item;

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
                for (; iter < toIter && iter < total; iter++) yield return (iter, percentInd);

                percentInd += 1;
            }
        }

        public static HashSet<T> Subtract<T>(this IEnumerable<T> set, IEnumerable<T> other)
        {
            var copy = new HashSet<T>(set);
            copy.ExceptWith(other);
            return copy;
        }

        public static void ApplyValue<T1, T2>(this IDictionary<T1, T2> dict, T1 key, Func<T2, T2> func)
        {
            var v = dict[key];
            dict[key] = func(v);
        }
    }
}
