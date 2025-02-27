using System.Collections.Generic;

namespace AutoPriorities
{
    public record SaveData
    {
        public HashSet<ExcludedPawnEntry> ExcludedPawns { get; init; } = new();

        public List<WorkTableEntry> WorkTablesData { get; init; } = new();

        public bool IgnoreLearningRate { get; init; }

        public float MinimumSkillLevel { get; init; }

        public bool IgnoreOppositionToWork { get; init; }

        public bool IgnoreWorkSpeed { get; init; }

        public bool RunOnTimer { get; set; }
    }
}
