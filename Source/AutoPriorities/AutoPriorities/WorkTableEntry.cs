using System.Collections.Generic;
using AutoPriorities.Core;
using AutoPriorities.Percents;
using AutoPriorities.Wrappers;

namespace AutoPriorities
{
    public struct WorkTableEntry
    {
        public Priority Priority { get; set; }

        public JobCount JobCount { get; set; }

        public Dictionary<IWorkTypeWrapper, TablePercent> WorkTypes { get; set; }
    }
}
