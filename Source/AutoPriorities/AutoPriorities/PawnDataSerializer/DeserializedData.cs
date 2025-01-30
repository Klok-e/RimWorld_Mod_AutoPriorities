using System.Collections.Generic;

namespace AutoPriorities.PawnDataSerializer
{
    public record DeserializedData
    {
        public HashSet<ExcludedPawnEntry> ExcludedPawns { get; init; } = new();

        public List<WorkTableEntry> WorkTablesData { get; init; } = new();
    }
}
