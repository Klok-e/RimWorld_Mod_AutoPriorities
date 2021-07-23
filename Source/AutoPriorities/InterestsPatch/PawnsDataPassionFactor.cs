using AutoPriorities;
using DInterests;
using HarmonyLib;
using RimWorld;

namespace InterestsPatch
{
    [HarmonyPatch(typeof(PawnsData), nameof(PawnsData.PassionFactor))]
    // ReSharper disable once UnusedType.Global
    public static class PawnsDataPassionFactor
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(ref float __result, Passion passion)
        {
            __result = InterestBase.LearnRateFactor(passion);
            return false;
        }
    }
}
