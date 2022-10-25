using System;

namespace AutoPriorities.Percents
{
    public readonly struct TablePercent
    {
        public PercentVariant Variant { get; }

        public double PercentValue { get; }

        public int NumberCount { get; }

        private TablePercent(PercentVariant variant, double percentValue, int numberCount)
        {
            Variant = variant;
            PercentValue = percentValue;
            NumberCount = numberCount;
        }

        public static TablePercent Percent(double value)
        {
            return new TablePercent(PercentVariant.Percent, value, 0);
        }

        public static TablePercent Number(int numberCount)
        {
            return new TablePercent(PercentVariant.Number, 0, numberCount);
        }

        public override string ToString()
        {
            return $"{nameof(Variant)}: {Variant}, " + $"{nameof(PercentValue)}: {PercentValue}, "
                                                     + $"{nameof(NumberCount)}: {NumberCount}";
        }
    }
}
