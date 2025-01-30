using System.Collections.Generic;

namespace AutoPriorities
{
    public record SaveDataRequest
    {
        public HashSet<ExcludedPawnEntry> ExcludedPawns { get; init; } = new();

        public List<WorkTableEntry> WorkTablesData { get; init; } = new();

        public bool IgnoreLearningRate { get; set; }

        public float MinimumSkillLevel { get; set; }

        public bool IgnoreOppositionToWork { get; set; }
    }
}
