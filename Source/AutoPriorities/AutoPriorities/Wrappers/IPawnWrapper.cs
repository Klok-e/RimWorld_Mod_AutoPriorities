namespace AutoPriorities.Wrappers
{
    public interface IPawnWrapper
    {
        string ThingID { get; }

        string NameFullColored { get; }

        string LabelNoCount { get; }

        bool IsCapableOfWholeWorkType(IWorkTypeWrapper work);

        double AverageOfRelevantSkillsFor(IWorkTypeWrapper work);

        float MaxLearningRateFactor(IWorkTypeWrapper work);

        void WorkSettingsSetPriority(IWorkTypeWrapper work, int priorityV);
    }
}
