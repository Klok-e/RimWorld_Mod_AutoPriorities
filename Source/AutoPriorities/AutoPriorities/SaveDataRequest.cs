using System.Collections.Generic;

namespace AutoPriorities
{
    public record SaveDataRequest
    {
        public HashSet<ExcludedPawnEntry> ExcludedPawns { get; init; } = null!;

        public List<WorkTableEntry> WorkTablesData { get; init; } = null!;
    }
}
