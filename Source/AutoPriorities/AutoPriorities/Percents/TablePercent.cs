using System;

namespace AutoPriorities.Percents
{
    public readonly struct TablePercent
    {
        private readonly double _percentValue;
        private readonly int _numberCount;
        public PercentVariant Variant { get; }

        public double PercentValue
        {
            get
            {
                if (Variant != PercentVariant.Percent)
                    throw new InvalidOperationException(
                        $"Tried to access {nameof(PercentValue)} but variant is: {Variant}");
                return _percentValue;
            }
        }

        public int NumberCount
        {
            get
            {
                if (Variant != PercentVariant.Number)
                    throw new InvalidOperationException(
                        $"Tried to access {nameof(NumberCount)} but variant is: {Variant}");
                return _numberCount;
            }
        }

        private TablePercent(PercentVariant variant, double percentValue, int numberCount)
        {
            Variant = variant;
            _percentValue = percentValue;
            _numberCount = numberCount;
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
            return $"{nameof(Variant)}: {Variant}, " + $"{nameof(PercentValue)}: {PercentValue}, "
                                                     + $"{nameof(NumberCount)}: {NumberCount}";
        }
    }
}
