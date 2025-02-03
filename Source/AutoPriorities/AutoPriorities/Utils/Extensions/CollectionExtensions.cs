using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoPriorities.Utils.Extensions
{
    public static class CollectionExtensions
    {
        public static void Shuffle<T>(this IList<T> list, Random rng)  
        {  
            var n = list.Count;  
            while (n > 1) {  
                n--;  
                var k = rng.Next(n + 1);  
                (list[k], list[n]) = (list[n], list[k]);
            }  
        }
        
        public static IEnumerable<T> Distinct<T, TK>(this IEnumerable<T> enumer, Func<T, TK> key)
        {
            var hashSet = new HashSet<TK>();
            foreach (var item in enumer)
            {
                if (!hashSet.Contains(key(item))) yield return item;

                hashSet.Add(key(item));
            }
        }

        public static int ArgMax<T>(this T[] span) where T : struct, IComparable<T>
        {
            if (span == null) throw new ArgumentNullException(nameof(span));

            var bestIndex = -1;
            T bestValue = default;
            var hasValue = false;
            var index = 0;

            for (var i = 0; i < span.Length; i++)
            {
                var item = span[i];
                if (!hasValue || item.CompareTo(bestValue) > 0)
                {
                    bestValue = item;
                    bestIndex = index;
                    hasValue = true;
                }

                index++;
            }

            if (bestIndex == -1) throw new InvalidOperationException("Sequence contains no elements.");
            return bestIndex;
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

        public static float[] ToFloat(this double[] array)
        {
            var result = new float[array.Length];
            for (var i = 0; i < array.Length; i++) result[i] = (float)array[i];

            return result;
        }

        public static double[] ToDouble(this float[] array)
        {
            var result = new double[array.Length];
            for (var i = 0; i < array.Length; i++) result[i] = array[i];

            return result;
        }

        public static IEnumerable<(int i, int percentIndex)> IterPercents(this IEnumerable<double> percents, int total)
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

        public static IEnumerable<MethodInfo> GetMethodsWithHelpAttribute<T>(this Assembly assembly) where T : Attribute
        {
            return assembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(type => type.GetCustomAttributes(typeof(T), true).Length > 0 && type.IsStatic);
        }
    }
}
