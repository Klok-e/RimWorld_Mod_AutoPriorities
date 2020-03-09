using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace AutoPriorities.Percents
{
    public class Number : IPercent
    {
        public RefInt? Total { get; set; }

        public int Count { get; }

        public Variant Variant => Variant.Number;
        public double Value => (double) Count / Total.Value;

        public Number(int count, RefInt? total)
        {
            Total = total;
            Count = count;
        }
    }
}