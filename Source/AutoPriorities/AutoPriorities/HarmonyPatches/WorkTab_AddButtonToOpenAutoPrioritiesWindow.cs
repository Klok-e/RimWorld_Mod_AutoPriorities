using AutoPriorities.Core;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace AutoPriorities.HarmonyPatches
{
    public static class WorkTab_AddButtonToOpenAutoPrioritiesWindow
    {
        private static void Postfix(MainTabWindow_Work __instance, Rect rect)
        {
            var window = Controller.Dialog;
            
            var button = new Rect(rect.x + 160, rect.y + 5, 25, 25);

            var col = Color.white;

            if (Verse.Widgets.ButtonImage(button, Core.Resources._autoPrioritiesButtonIcon, col, col * 0.9f))
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