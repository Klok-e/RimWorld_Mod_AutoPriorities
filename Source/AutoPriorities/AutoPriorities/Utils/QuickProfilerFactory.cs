using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AutoPriorities.Utils
{
    public class QuickProfilerFactory
    {
        private List<QuickProfiler> _profilers = new();

        public void SaveProfileData()
        {
            var str = new StringBuilder();
            foreach (var profiler in _profilers)
                str.Append($"{profiler.name} took {profiler.sw.Elapsed.TotalMilliseconds}ms\n");

            File.WriteAllText("/tmp/auto-priority-profile.txt", str.ToString());
        }

        public QuickProfiler CreateProfiler(string name)
        {
            var profiler = new QuickProfiler(name);
            _profilers.Add(profiler);
            return profiler;
        }
    }
}