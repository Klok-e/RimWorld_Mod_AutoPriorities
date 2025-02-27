using Verse;

namespace AutoPriorities.Wrappers
{
    public interface IWorkTypeWrapper
    {
        WorkTypeDef? WorkTypeDef { get; }

        string DefName { get; }

        WorkTags WorkTags { get; }

        int RelevantSkillsCount { get; }

        string LabelShort { get; }

        WorkTypeDef GetWorkTypeDefOrThrow();
    }
}
