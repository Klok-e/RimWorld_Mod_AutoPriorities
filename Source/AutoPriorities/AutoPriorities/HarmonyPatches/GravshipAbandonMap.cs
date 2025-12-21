using AutoPriorities.Core;
using HarmonyLib;
using RimWorld.Planet;

namespace AutoPriorities.HarmonyPatches
{
    [HarmonyPatch(typeof(MapParent), nameof(MapParent.Abandon))]
    // ReSharper disable once UnusedType.Global
    public static class GravshipAbandonMap
    {
        [HarmonyPostfix]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        private static void Prefix(MapParent __instance)
        {
            var dataOfAbandonedMap = __instance.Map.GetComponent<MapSpecificData>();

            if (Controller.DebugLogs)
                Controller.logger?.Info($"Map abandoned: {__instance.GetInspectString()}");

            Controller.AbandonedMapMapSpecificData = dataOfAbandonedMap;
        }
    }
}
