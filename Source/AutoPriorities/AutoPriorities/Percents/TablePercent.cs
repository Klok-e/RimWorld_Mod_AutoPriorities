using System;

namespace AutoPriorities.Percents
{
    public readonly struct TablePercent
    {
        public PercentVariant Variant { get; }

        public double Value =>
            Variant switch
            {
                PercentVariant.Percent => PercentValue,
                PercentVariant.Number => (double)NumberCount / NumberTotal,
                _ => throw new ArgumentOutOfRangeException()
            };

        public double PercentValue { get; }

        public int NumberTotal { get; }

        public int NumberCount { get; }

        private TablePercent(PercentVariant variant, double percentValue, int numberTotal, int numberCount)
        {
            Variant = variant;
            PercentValue = percentValue;
            NumberTotal = numberTotal;
            NumberCount = numberCount;
        }

        public static TablePercent Percent(double value)
        {
            return new(PercentVariant.Percent, value, 0, 0);
        }

        public static TablePercent Number(int numberTotal, int numberCount)
        {
            return new(PercentVariant.Number, 0, numberTotal, numberCount);
        }

        public override string ToString()
        {
            return $"{nameof(Variant)}: {Variant}, "
                   + $"{nameof(PercentValue)}: {PercentValue}, "
                   + $"{nameof(NumberTotal)}: {NumberTotal}, "
                   + $"{nameof(NumberCount)}: {NumberCount}";
        }
    }
}
