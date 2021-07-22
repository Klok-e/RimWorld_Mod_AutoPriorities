using AutoPriorities.Utils;

namespace AutoPriorities.Percents
{
    public class Number : IPercent, IPoolable<Number, NumberPoolArgs>
    {
        public int Total { get; private set; }

        public int Count { get; private set; }

        #region IPercent Members

        public double Value => (double)Count / Total;

        #endregion

        #region IPoolable<Number,NumberPoolArgs> Members

        public Number Initialize(NumberPoolArgs args)
        {
            Total = args.Total;
            Count = args.Count;
            return this;
        }

        public void Deinitialize()
        {
        }

        #endregion
    }

    public struct NumberPoolArgs : IPoolArgs
    {
        public int Count { get; set; }

        public int Total { get; set; }
    }
}
