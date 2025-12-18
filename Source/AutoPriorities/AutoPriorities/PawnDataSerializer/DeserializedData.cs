using System.Collections.Generic;

namespace AutoPriorities.PawnDataSerializer
{
    public record DeserializedData
    {
        public List<WorkTableEntry> WorkTablesData { get; init; } = new();
    }
}
