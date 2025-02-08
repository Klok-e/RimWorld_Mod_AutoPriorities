using System.Collections.Generic;
using System.Linq;
using AutoPriorities;
using AutoPriorities.Core;
using BetterPawnControl;
using HarmonyLib;
using Verse;

namespace BetterPawnControlPatch
{
    // ReSharper disable once UnusedType.Global
    public static class BetterPawnControlPatch
    {
        [PatchInitialize]
        // ReSharper disable once UnusedMember.Global
        public static void Init()
        {
            Controller.SetPrioritiesOnTimerCallback += () =>
            {
                var workManagerType = AccessTools.TypeByName("BetterPawnControl.WorkManager");
                if (workManagerType == null)
                {
                    Controller.logger?.Err("BetterPawnControlPatch: workManager is null");
                    return;
                }

                var colonistsMethod = AccessTools.Method(workManagerType, "Colonists");
                if (!(colonistsMethod?.Invoke(null, null) is IEnumerable<Pawn> colonists))
                {
                    Controller.logger?.Err("BetterPawnControlPatch: Could not find Colonists");
                    return;
                }

                var colonistsList = colonists.ToList();

                var saveMethod = AccessTools.Method(workManagerType, "SaveCurrentState");
                saveMethod?.Invoke(null, new object[] { colonistsList });

                var cleanupMethod = AccessTools.Method(workManagerType, "LinksCleanUp");
                cleanupMethod?.Invoke(null, null);

                Widget_WorkTab.ClearCache();
            };
        }
    }
}
