using Verse;

namespace AutoPriorities.Extensions
{
    public static class PawnExtensions
    {
        public static bool IsCapableOfWholeWorkType(this Pawn pawn, WorkTypeDef workType)
        {
            return !pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(workType.workTags);
        }
    }
}
