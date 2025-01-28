using AutoPriorities;
using HarmonyLib;
using UnityEngine;
using Verse;
using WorkTab;
using Controller = AutoPriorities.Core.Controller;
using Resources = AutoPriorities.Core.Resources;

namespace FluffyWorktabPatch
{
    [HarmonyPatch(typeof(MainTabWindow_WorkTab), nameof(MainTabWindow_WorkTab.DoWindowContents))]
    // ReSharper disable once UnusedType.Global
    public static class WorkTabAddButtonToFluffysWorktab
    {
        [PatchInitialize]
        // ReSharper disable once UnusedMember.Global
        public static void Init()
        {
            Controller.MaxPriorityAlien = Settings.maxPriority;
        }

        [HarmonyPostfix]
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(Rect rect)
        {
            var window = Controller.Dialog;
            if (window == null) return;

            var button = new Rect(rect.x + 160, rect.y + 5, 25, 25);

            var col = Color.white;

            if (!Widgets.ButtonImage(button, Resources.AutoPrioritiesButtonIcon, col, col * 0.9f)) return;

            if (!window.IsOpen)
                Find.WindowStack.Add(window);
            else
                window.Close();
        }
    }
}
