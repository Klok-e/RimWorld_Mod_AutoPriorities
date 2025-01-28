using AutoPriorities.Wrappers;
using Verse;

namespace Tests.MockImplementations
{
    public record WorkType : IWorkTypeWrapper
    {
        #region IWorkTypeWrapper Members

        public string DefName { get; init; } = string.Empty;

        public WorkTags WorkTags { get; init; }

        public int RelevantSkillsCount { get; init; }

        public string LabelShort { get; init; } = string.Empty;

        #endregion
    }
}
