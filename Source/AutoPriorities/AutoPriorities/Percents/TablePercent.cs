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

        public TablePercent(PercentVariant variant, double percentValue, int numberTotal, int numberCount)
        {
            Variant = variant;
            PercentValue = percentValue;
            NumberTotal = numberTotal;
            NumberCount = numberCount;
        }

        public static TablePercent Percent(double value)
        {
            return new TablePercent(PercentVariant.Percent, value, 0, 0);
        }
        
        public static TablePercent Number(int numberTotal, int numberCount)
        {
            return new TablePercent(PercentVariant.Percent, 0, numberTotal, numberCount);
        }
    }
}
