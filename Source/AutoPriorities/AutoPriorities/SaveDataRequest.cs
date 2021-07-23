using System.Collections.Generic;
using AutoPriorities.Core;
using AutoPriorities.Percents;
using AutoPriorities.Wrappers;

namespace AutoPriorities
{
    public record SaveDataRequest
    {
        public HashSet<(IWorkTypeWrapper, IPawnWrapper)> ExcludedPawns { get; init; } = null!;

        public List<(Priority priority, JobCount maxJobs, Dictionary<IWorkTypeWrapper, IPercent> workTypes)>
            WorkTablesData { get; init; } = null!;
    }
}
