using Verse;

namespace AutoPriorities.Wrappers
{
    public interface IPawnWrapper
    {
        Pawn? Pawn { get; }

        string ThingID { get; }

        string NameFullColored { get; }

        string LabelNoCount { get; }

        Pawn GetPawnOrThrow();

        bool IsCapableOfWholeWorkType(IWorkTypeWrapper work);

        bool IsOpposedToWorkType(IWorkTypeWrapper work);
        bool IsIncapacitated();

        bool IsAdult();

        float AverageOfRelevantSkillsFor(IWorkTypeWrapper work);

        float MaxLearningRateFactor(IWorkTypeWrapper work);

        int WorkSettingsGetPriority(IWorkTypeWrapper work);

        void WorkSettingsSetPriority(IWorkTypeWrapper work, int priorityV);
    }
}
