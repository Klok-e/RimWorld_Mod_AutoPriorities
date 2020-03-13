using HarmonyLib;
using RimWorld;
using UnityEngine;
using WorkTab;

namespace FluffyWorktabPatch
{
    [HarmonyPatch(typeof(MainTabWindow_WorkTab), nameof(MainTabWindow_WorkTab.DoWindowContents))]
    public static class WorkTab_AddButtonToFluffysWorktab
    {
        private static void Postfix(MainTabWindow_WorkTab __instance, Rect rect)
        {
            var window = AutoPriorities.Core.Controller.Dialog;

            var button = new Rect(rect.x + 160, rect.y + 5, 25, 25);

            var col = Color.white;

            if (Verse.Widgets.ButtonImage(button, AutoPriorities.Core.Resources._autoPrioritiesButtonIcon, col,
                col * 0.9f))
            {
                if (!window.IsOpen)
                {
                    Verse.Find.WindowStack.Add(window);
                }
                else
                {
                    window.Close();
                }
            }
        }
    }
}