using System;
using System.Linq;
using Verse;

namespace AutoPriorities.Wrappers
{
    public record PawnWrapper : IPawnWrapper
    {
        public PawnWrapper(Pawn pawn)
        {
            Pawn = pawn;
        }

        public Pawn? Pawn { get; }

        #region IPawnWrapper Members

        public string ThingID => GetPawnOrThrow().ThingID;

        public Pawn GetPawnOrThrow()
        {
            return Pawn ?? throw new NullReferenceException(nameof(Pawn));
        }

        public string NameFullColored => GetPawnOrThrow().NameFullColored;

        public string LabelNoCount => GetPawnOrThrow().LabelNoCount;

        public bool IsCapableOfWholeWorkType(IWorkTypeWrapper work)
        {
            return !GetPawnOrThrow().WorkTypeIsDisabled(work.WorkTypeDef);
        }

        public bool IsOpposedToWorkType(IWorkTypeWrapper work)
        {
            return GetPawnOrThrow().Ideo?.IsWorkTypeConsideredDangerous(work.WorkTypeDef) == true;
        }

        public float AverageOfRelevantSkillsFor(IWorkTypeWrapper work)
        {
            return GetPawnOrThrow().skills.AverageOfRelevantSkillsFor(work.WorkTypeDef);
        }

        public float MaxLearningRateFactor(IWorkTypeWrapper work)
        {
            var factor = (work.WorkTypeDef ?? throw new NullReferenceException(nameof(work.WorkTypeDef))).relevantSkills
                .Select(GetPawnOrThrow().skills.GetSkill)
                .Select(x => x.LearnRateFactor())
                .DefaultIfEmpty(1)
                .Max();

            return factor;
        }

        public void WorkSettingsSetPriority(IWorkTypeWrapper work, int priorityV)
        {
            GetPawnOrThrow()
                .workSettings.SetPriority(work.WorkTypeDef ?? throw new NullReferenceException(nameof(work.WorkTypeDef)), priorityV);
        }

        #endregion
    }
}
