using AutoPriorities.Core;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Resources = AutoPriorities.Core.Resources;

namespace AutoPriorities.HarmonyPatches
{
    [HarmonyPatch(typeof(MainTabWindow_Work), nameof(MainTabWindow_Work.DoWindowContents))]
    public static class WorkTab_AddButtonToOpenAutoPrioritiesWindow
    {
        [HarmonyPostfix]
        private static void Postfix(Rect rect)
        {
            var window = Controller.Dialog;

            var button = new Rect(rect.x + 160, rect.y + 5, 25, 25);

            var col = Color.white;

            if (!Widgets.ButtonImage(button, Resources._autoPrioritiesButtonIcon, col, col * 0.9f)) return;

            if (!window.IsOpen)
                Find.WindowStack.Add(window);
            else
                window.Close();
        }
    }
}
