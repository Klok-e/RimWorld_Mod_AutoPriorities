using System;
using System.Diagnostics;

namespace AutoPriorities.Utils
{
    public class QuickProfiler : IDisposable
    {
        public readonly Stopwatch sw = Stopwatch.StartNew();
        public readonly string name;

        public QuickProfiler(string name)
        {
            this.name = name;
        }

        public void Dispose()
        {
            sw.Stop();
        }
    }
}
