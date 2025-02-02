using AutoPriorities.Wrappers;
using Verse;

namespace AutoPriorities.SerializableSimpleData
{
    public record WorkTypeSimpleData
    {
        public string? defName;
        public string? labelShort;
        public int relevantSkillsCount;
        public WorkTags workTags;

        public WorkTypeSimpleData()
        {
        }

        public WorkTypeSimpleData(IWorkTypeWrapper workTypeWrapper)
        {
            defName = workTypeWrapper.DefName;
            workTags = workTypeWrapper.WorkTags;
            relevantSkillsCount = workTypeWrapper.RelevantSkillsCount;
            labelShort = workTypeWrapper.LabelShort;
        }
    }
}
