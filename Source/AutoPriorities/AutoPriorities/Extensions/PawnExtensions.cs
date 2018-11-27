using Verse;

namespace AutoPriorities.Extensions
{
    public static class PawnExtensions
    {
        public static bool IsCapableOfWholeWorkType(this Pawn pawn, WorkTypeDef workType)
        {
            return !pawn.story.WorkTypeIsDisabled(workType);
        }
    }
}
