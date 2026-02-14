using System.Collections.Generic;

namespace AutoPriorities.Core
{
    public interface IMapSpecificData
    {
        List<string>? ImportantWorkTypes { get; set; }
        byte[]? PawnsDataXml { get; set; }
        float MinimumSkillLevel { get; set; }

        bool IgnoreLearningRate { get; set; }
        bool IgnoreOppositionToWork { get; set; }
        bool IgnoreDownedStatus { get; set; }
        bool IgnoreWorkSpeed { get; set; }
        bool ForbidNonAdultsFromSelectedJobs { get; set; }
        bool RunOnTimer { get; set; }
        bool HasOpenedDialogOnce { get; set; }
    }
}
