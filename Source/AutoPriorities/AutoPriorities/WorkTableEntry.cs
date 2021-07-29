using System.Collections.Generic;
using AutoPriorities.Core;
using AutoPriorities.Percents;
using AutoPriorities.Wrappers;

namespace AutoPriorities
{
    public struct WorkTableEntry
    {
        public Priority priority { get; set; }

        public JobCount jobCount { get; set; }

        public Dictionary<IWorkTypeWrapper, TablePercent> workTypes { get; set; }
    }
}
