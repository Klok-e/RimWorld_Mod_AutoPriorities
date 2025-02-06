using System;
using Verse;

namespace AutoPriorities.Wrappers
{
    public record WorkTypeWrapper : IWorkTypeWrapper
    {
        public WorkTypeWrapper(WorkTypeDef workWorkTypeDef)
        {
            WorkTypeDef = workWorkTypeDef;
        }

        public WorkTypeDef? WorkTypeDef { get; }

        #region IWorkTypeWrapper Members

        public WorkTypeDef GetWorkTypeDefOrThrow()
        {
            return WorkTypeDef ?? throw new NullReferenceException(nameof(WorkTypeDef));
        }

        public string DefName => GetWorkTypeDefOrThrow().defName;

        public WorkTags WorkTags => GetWorkTypeDefOrThrow().workTags;

        public int RelevantSkillsCount => GetWorkTypeDefOrThrow().relevantSkills.Count;

        public string LabelShort => GetWorkTypeDefOrThrow().labelShort.CapitalizeFirst();

        #endregion
    }
}
