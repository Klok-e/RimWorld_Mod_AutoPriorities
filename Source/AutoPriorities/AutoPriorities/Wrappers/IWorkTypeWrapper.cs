using Verse;

namespace AutoPriorities.Wrappers
{
    public interface IWorkTypeWrapper
    {
        string defName { get; }

        WorkTags workTags { get; }

        int relevantSkillsCount { get; }

        string labelShort { get; }
    }
}
