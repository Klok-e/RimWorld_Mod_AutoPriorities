using AutoPriorities.Wrappers;
using Verse;

namespace Tests.MockImplementations
{
    public class WorkType : IWorkTypeWrapper
    {
        public WorkType(string defName, WorkTags workTags, int relevantSkillsCount, string labelShort)
        {
            this.defName = defName;
            this.workTags = workTags;
            this.relevantSkillsCount = relevantSkillsCount;
            this.labelShort = labelShort;
        }

        #region IWorkTypeWrapper Members

        public string defName { get; }

        public WorkTags workTags { get; }

        public int relevantSkillsCount { get; }

        public string labelShort { get; }

        #endregion
    }
}
