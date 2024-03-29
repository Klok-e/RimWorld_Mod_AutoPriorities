using System.Collections.Generic;

namespace AutoPriorities
{
    public record SaveData
    {
        public HashSet<ExcludedPawnEntry> ExcludedPawns { get; init; } = new();

        public List<WorkTableEntry> WorkTablesData { get; init; } = new();
    }
}
