using Verse;

namespace AutoPriorities.Wrappers
{
    internal class WorkTypeWrapper : IWorkTypeWrapper
    {
        public readonly WorkTypeDef workTypeDef;

        public WorkTypeWrapper(WorkTypeDef workTypeDef)
        {
            this.workTypeDef = workTypeDef;
        }

        #region IWorkTypeWrapper Members

        public string defName => workTypeDef.defName;

        public WorkTags workTags => workTypeDef.workTags;

        public int relevantSkillsCount => workTypeDef.relevantSkills.Count;

        public string labelShort => workTypeDef.labelShort;

        #endregion
    }
}
