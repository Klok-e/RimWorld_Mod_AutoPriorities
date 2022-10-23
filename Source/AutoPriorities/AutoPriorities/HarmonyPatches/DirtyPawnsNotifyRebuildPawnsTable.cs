using AutoPriorities.Core;
using HarmonyLib;
using RimWorld;
using Verse;
using Resources = AutoPriorities.Core.Resources;

namespace AutoPriorities.HarmonyPatches
{
    [HarmonyPatch(typeof(MainTabWindowUtility), nameof(MainTabWindowUtility.NotifyAllPawnTables_PawnsChanged))]
    // ReSharper disable once UnusedType.Global
    public static class DirtyPawnsNotifyRebuildPawnsTable
    {
        [HarmonyPostfix]
        // ReSharper disable once UnusedMember.Local
        private static void Postfix()
        {
            Controller.pawnData?.Rebuild();
        }
    }
}
