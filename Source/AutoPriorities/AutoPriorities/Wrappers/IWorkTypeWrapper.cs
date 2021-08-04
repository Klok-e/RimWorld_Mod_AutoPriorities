using Verse;

namespace AutoPriorities.Wrappers
{
    public interface IWorkTypeWrapper
    {
        string DefName { get; }

        WorkTags WorkTags { get; }

        int RelevantSkillsCount { get; }

        string LabelShort { get; }
    }
}
