using System.Linq;
using Verse;

namespace AutoPriorities.Wrappers
{
    public record PawnWrapper : IPawnWrapper
    {
        private readonly Pawn _pawn;

        public PawnWrapper(Pawn pawn)
        {
            _pawn = pawn;
        }

        #region IPawnWrapper Members

        public string ThingID => _pawn.ThingID;

        public string NameFullColored => _pawn.NameFullColored;

        public string LabelNoCount => _pawn.LabelNoCount;

        public bool IsCapableOfWholeWorkType(IWorkTypeWrapper work)
        {
            return !_pawn.WorkTypeIsDisabled(((WorkTypeWrapper)work).workTypeDef);
        }

        public bool IsOpposedToWorkType(IWorkTypeWrapper work)
        {
            return _pawn.Ideo?.IsWorkTypeConsideredDangerous(((WorkTypeWrapper)work).workTypeDef) == true;
        }

        public float AverageOfRelevantSkillsFor(IWorkTypeWrapper work)
        {
            return _pawn.skills.AverageOfRelevantSkillsFor(((WorkTypeWrapper)work).workTypeDef);
        }

        public float MaxLearningRateFactor(IWorkTypeWrapper work)
        {
            var factor = ((WorkTypeWrapper)work).workTypeDef.relevantSkills.Select(_pawn.skills.GetSkill)
                .Select(x => x.LearnRateFactor())
                .DefaultIfEmpty(1)
                .Max();

            return factor;
        }

        public void WorkSettingsSetPriority(IWorkTypeWrapper work, int priorityV)
        {
            _pawn.workSettings.SetPriority(((WorkTypeWrapper)work).workTypeDef, priorityV);
        }

        #endregion
    }
}
