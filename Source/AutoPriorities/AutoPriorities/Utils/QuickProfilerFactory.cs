using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutoPriorities.Utils
{
    public class QuickProfilerFactory
    {
        private List<QuickProfiler> _profilers = new();

        public void SaveProfileData()
        {
            var str = _profilers.Aggregate(string.Empty,
                (current, profile) => current + $"{profile} took {profile.sw.ElapsedMilliseconds}ms\n");

            File.WriteAllText("/tmp/auto-priority-profile.txt", str);
        }

        public QuickProfiler CreateProfiler(string name)
        {
            var profiler = new QuickProfiler(name);
            _profilers.Add(profiler);
            return profiler;
        }
    }
}
