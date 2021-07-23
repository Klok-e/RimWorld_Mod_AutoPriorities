using AutoPriorities.Wrappers;
using Verse;

namespace AutoPriorities.Extensions
{
    public static class PawnExtensions
    {
        public static bool IsCapableOfWholeWorkType(this Pawn pawn, IWorkTypeWrapper workType)
        {
            var capable = (pawn.CombinedDisabledWorkTags & workType.workTags) == WorkTags.None;
#if DEBUG
            //Log.Message($"{pawn.NameFullColored} is {(capable ? "" : "not")} capable of {workType.defName}");
#endif
            return capable;
        }
    }
}
