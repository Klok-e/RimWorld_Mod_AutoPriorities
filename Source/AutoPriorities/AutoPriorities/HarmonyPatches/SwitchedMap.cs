using AutoPriorities.Core;
using HarmonyLib;
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
        private static void Postfix(Map value)
        {
            if (Controller.DebugLogs)
                Controller.logger?.Info($"Switched map by notification. Map value: {value}");

            Controller.SwitchMap();
        }
    }
}
