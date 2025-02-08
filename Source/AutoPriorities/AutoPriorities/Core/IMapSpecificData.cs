using System.Collections.Generic;

namespace AutoPriorities.Core
{
    public interface IMapSpecificData
    {
        public List<string>? ImportantWorkTypes { get; set; }
        public byte[]? PawnsDataXml { get; set; }
        public float MinimumSkillLevel { get; set; }
        public List<ExcludedPawnEntry> ExcludedPawns { get; set; }
        public bool IgnoreLearningRate { get; set; }
        bool IgnoreOppositionToWork { get; set; }
        bool IgnoreWorkSpeed { get; set; }
        bool RunOncePerDay { get; set; }
    }
}
