using AutoPriorities;
using DInterests;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;


namespace InterestsPatch
{
    [HarmonyPatch(typeof(PawnsData), nameof(PawnsData.PassionFactor))]
    public static class PawnsData_PassionFactor
    {
        
        private static bool Prefix(ref float __result, Passion passion)
        {
            __result = InterestBase.LearnRateFactor(passion);
            return false;
        }
    }
}