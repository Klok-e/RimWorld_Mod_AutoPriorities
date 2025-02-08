using AutoPriorities.Core;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AutoPriorities.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.SetPriority))]
    // ReSharper disable once UnusedType.Global
    public static class PawnWorkSettingsSetPriorityPatch
    {
        [HarmonyPostfix]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        private static void Postfix(WorkTypeDef w, int priority)
        {
            if (Controller.AnnonyingDebugLogs)
                Controller.logger?.Info($"Priority for {w.defName} set to {priority}");
        }
    }
}
