using AutoPriorities.Core;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Resources = AutoPriorities.Core.Resources;

namespace AutoPriorities.HarmonyPatches
{
    [HarmonyPatch(typeof(MainTabWindow_Work), nameof(MainTabWindow_Work.DoWindowContents))]
    // ReSharper disable once UnusedType.Global
    public static class WorkTabAddButtonToOpenAutoPrioritiesWindow
    {
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
