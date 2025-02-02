using System;
using System.Collections.Generic;
using System.Linq;
using AutoPriorities.Core;
using AutoPriorities.Percents;
using AutoPriorities.Wrappers;

namespace AutoPriorities
{
    [Serializable]
    public struct WorkTableEntry
    {
        public Priority Priority { get; set; }

        public JobCount JobCount { get; set; }

        public Dictionary<IWorkTypeWrapper, TablePercent> WorkTypes { get; set; }

        public WorkTableEntry ShallowCopy()
        {
            return new WorkTableEntry
            {
                Priority = Priority, JobCount = JobCount, WorkTypes = WorkTypes.ToDictionary(x => x.Key, x => x.Value),
            };
        }
    }
}
