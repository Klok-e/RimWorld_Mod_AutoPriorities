using System.Collections.Generic;
using AutoPriorities.Wrappers;

namespace AutoPriorities
{
    public record SaveDataRequest
    {
        public HashSet<(IWorkTypeWrapper, IPawnWrapper)> ExcludedPawns { get; init; } = null!;

        public List<WorkTableEntry> WorkTablesData { get; init; } = null!;
    }
}
