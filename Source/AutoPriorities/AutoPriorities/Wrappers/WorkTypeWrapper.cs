using Verse;

namespace AutoPriorities.Wrappers
{
    internal record WorkTypeWrapper : IWorkTypeWrapper
    {
        public readonly WorkTypeDef workTypeDef;

        public WorkTypeWrapper(WorkTypeDef workTypeDef)
        {
            this.workTypeDef = workTypeDef;
        }

        #region IWorkTypeWrapper Members

        public string DefName => workTypeDef.defName;

        public WorkTags WorkTags => workTypeDef.workTags;

        public int RelevantSkillsCount => workTypeDef.relevantSkills.Count;

        public string LabelShort => workTypeDef.labelShort;

        #endregion
    }
}
