using AutoPriorities.Wrappers;
using Verse;

namespace Tests.MockImplementations
{
    public record WorkType : IWorkTypeWrapper
    {
        #region IWorkTypeWrapper Members

        public string DefName { get; set; } = string.Empty;

        public WorkTags WorkTags { get; set; }

        public int RelevantSkillsCount { get; set; }

        public string LabelShort { get; set; } = string.Empty;

        #endregion
    }
}
