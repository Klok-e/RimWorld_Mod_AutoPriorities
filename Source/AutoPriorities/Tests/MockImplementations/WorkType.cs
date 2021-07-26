using AutoPriorities.Wrappers;
using Verse;

namespace Tests.MockImplementations
{
    public record WorkType : IWorkTypeWrapper
    {
        #region IWorkTypeWrapper Members

        public string defName { get; set; } = string.Empty;

        public WorkTags workTags { get; set; }

        public int relevantSkillsCount { get; set; }

        public string labelShort { get; set; } = string.Empty;

        #endregion
    }
}
