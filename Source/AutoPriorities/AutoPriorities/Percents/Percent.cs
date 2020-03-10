using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace AutoPriorities.Percents
{
    public class Percent : IPercent
    {
        public Variant Variant => Variant.Percent;

        public double Value { get; }

        public Percent(double value)
        {
            Value = value;
        }
    }
}