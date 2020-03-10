using UnityEngine;
using Verse;

namespace AutoPriorities.Extensions
{
    public static class PawnExtensions
    {
        public static bool IsCapableOfWholeWorkType(this Pawn pawn, WorkTypeDef workType)
        {
            var capable = (pawn.story.DisabledWorkTagsBackstoryAndTraits & workType.workTags) == WorkTags.None;
#if DEBUG
            //Log.Message($"{pawn.NameFullColored} is {(capable ? "" : "not")} capable of {workType.defName}");
#endif
            return capable;
        }
    }
}