using System;
using System.Diagnostics;

namespace AutoPriorities.Utils
{
    public class QuickProfiler : IDisposable
    {
        public readonly string name;
        public readonly Stopwatch sw = Stopwatch.StartNew();

        public QuickProfiler(string name)
        {
            this.name = name;
        }

        #region IDisposable Members

        public void Dispose()
        {
            sw.Stop();
        }

        #endregion
    }
}
