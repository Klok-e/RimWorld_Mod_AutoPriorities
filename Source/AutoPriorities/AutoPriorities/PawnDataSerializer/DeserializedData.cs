using System;
using System.Collections.Generic;

namespace AutoPriorities.PawnDataSerializer
{
    public record DeserializedData
    {
        [Obsolete("Backwards compatibility only")]
        public HashSet<ExcludedPawnEntry> ExcludedPawns { get; init; } = new();

        public List<WorkTableEntry> WorkTablesData { get; init; } = new();
    }
}
