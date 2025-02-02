using AutoPriorities.Wrappers;

namespace AutoPriorities.SerializableSimpleData
{
    public class PawnWorkTypeData
    {
        public double averageOfRelevantSkillsFor;

        public bool isCapableOfWholeWorkType;

        public bool isOpposedToWorkType;

        public float maxLearningRateFactor;

        public string workTypeDefName = null!;

        public PawnWorkTypeData()
        {
        }

        public PawnWorkTypeData(IPawnWrapper pawnWrapper, IWorkTypeWrapper workTypeWrapper)
        {
            isCapableOfWholeWorkType = pawnWrapper.IsCapableOfWholeWorkType(workTypeWrapper);
            isOpposedToWorkType = pawnWrapper.IsOpposedToWorkType(workTypeWrapper);
            averageOfRelevantSkillsFor = pawnWrapper.AverageOfRelevantSkillsFor(workTypeWrapper);
            maxLearningRateFactor = pawnWrapper.MaxLearningRateFactor(workTypeWrapper);
            workTypeDefName = workTypeWrapper.DefName;
        }
    }
}
