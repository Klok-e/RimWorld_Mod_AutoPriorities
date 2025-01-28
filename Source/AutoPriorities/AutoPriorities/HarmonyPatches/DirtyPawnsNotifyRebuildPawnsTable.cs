using AutoPriorities.Core;
using AutoPriorities.Ui;
using HarmonyLib;
using RimWorld;
using Verse;

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
            var windowStack = Find.WindowStack;
            if (windowStack.WindowOfType<AutoPrioritiesDialog>() is not null)
            {
#if DEBUG
                Controller.logger?.Info("Rebuild by notification");
#endif
                Controller.RebuildPawns();
            }
            else
            {
#if DEBUG
                Controller.logger?.Info("Rebuild by notification: auto priority window not found");
#endif
            }
        }
    }
}
