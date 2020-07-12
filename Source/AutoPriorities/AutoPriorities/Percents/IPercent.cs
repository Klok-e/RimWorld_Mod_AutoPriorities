using System;

namespace AutoPriorities.Percents
{
    public interface IPercent
    {
        [Obsolete]
        Variant Variant { get; }

        double Value { get; }
    }
}