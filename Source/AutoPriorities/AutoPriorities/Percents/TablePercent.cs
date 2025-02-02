using System;
using UnityEngine;

namespace AutoPriorities.Percents
{
    [Serializable]
    public struct TablePercent
    {
        public readonly double percentValue;
        public readonly int numberCount;
        [SerializeField] public PercentVariant variant;

        public double PercentValue
        {
            get
            {
                if (variant != PercentVariant.Percent)
                    throw new InvalidOperationException($"Tried to access {nameof(PercentValue)} but variant is: {variant}");
                return percentValue;
            }
        }

        public int NumberCount
        {
            get
            {
                if (variant != PercentVariant.Number)
                    throw new InvalidOperationException($"Tried to access {nameof(NumberCount)} but variant is: {variant}");
                return numberCount;
            }
        }

        private TablePercent(PercentVariant variant, double percentValue, int numberCount)
        {
            this.variant = variant;
            this.percentValue = percentValue;
            this.numberCount = numberCount;
        }

        public static TablePercent Percent(double value)
        {
            return new TablePercent(PercentVariant.Percent, value, 0);
        }

        public static TablePercent Number(int numberCount)
        {
            return new TablePercent(PercentVariant.Number, 0, numberCount);
        }

        public static TablePercent PercentRemaining()
        {
            return new TablePercent(PercentVariant.PercentRemaining, 0, 0);
        }

        public override string ToString()
        {
            return $"{nameof(variant)}: {variant}, "
                   + $"{nameof(PercentValue)}: {PercentValue}, "
                   + $"{nameof(NumberCount)}: {NumberCount}";
        }
    }
}
