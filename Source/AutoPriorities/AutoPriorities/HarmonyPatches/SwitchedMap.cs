using AutoPriorities.Core;
using AutoPriorities.Ui;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AutoPriorities.HarmonyPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.CurrentMap))]
    [HarmonyPatch(MethodType.Setter)]
    // ReSharper disable once UnusedType.Global
    public static class SwitchedMap
    {
        [HarmonyPostfix]
        // ReSharper disable once UnusedMember.Local
        private static void Postfix()
        {
#if DEBUG
            Controller.logger?.Info("Switched map by notification");
#endif
            Controller.SwitchMap();
        }
    }
}
