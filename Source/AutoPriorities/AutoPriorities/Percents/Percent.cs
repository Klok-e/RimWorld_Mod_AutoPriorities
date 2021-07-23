using AutoPriorities.Utils;

namespace AutoPriorities.Percents
{
    public class Percent : IPercent, IPoolable<Percent, PercentPoolArgs>
    {
        #region IPercent Members

        public double Value { get; private set; }

        #endregion

        #region IPoolable<Percent,PercentPoolArgs> Members

        public Percent Initialize(PercentPoolArgs args)
        {
            Value = args.Value;
            return this;
        }

        public void Deinitialize()
        {
        }

        #endregion
    }

    public struct PercentPoolArgs
    {
        public double Value { get; set; }
    }
}
