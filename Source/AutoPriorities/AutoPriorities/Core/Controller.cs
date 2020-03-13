using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using System.Reflection;
using AutoPriorities.HarmonyPatches;
using AutoPriorities.Utils;
using HugsLib;
using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoPriorities.Core
{
    public class Controller : ModBase
    {
        public static ModLogger Log { get; private set; }

        private static AutoPriorities_Dialog _dialog;
        public static AutoPriorities_Dialog Dialog => _dialog ??= new AutoPriorities_Dialog();

        public override void Initialize()
        {
            base.Initialize();
            Log = Logger;
            PatchWorkTab();
        }

        private void PatchWorkTab()
        {
            var classInstance = (from asm in AppDomain.CurrentDomain.GetAssemblies()
                from type in asm.GetTypes()
                where type.IsClass && type.Name == "MainTabWindow_WorkTab"
                select type).SingleOrDefault();

            Type worktab;
            Type patchClass;
            if (classInstance == null)
            {
                // no fluffy's worktab detected
#if DEBUG
                Log.Message("No Fluffy's worktab detected");
#endif
                worktab = typeof(MainTabWindow_Work);
                patchClass = typeof(WorkTab_AddButtonToOpenAutoPrioritiesWindow);
            }
            else
            {
                // fluffy's worktab
#if DEBUG
                Log.Message("Fluffy's worktab detected");
#endif
                Assembly.LoadFile(Path.Combine(ModContentPack.RootDir,
                    "ConditionalAssemblies/1.1/FluffyWorktabPatch.dll"));

                worktab = (from asm in AppDomain.CurrentDomain.GetAssemblies()
                    from type in asm.GetTypes()
                    where type.IsClass && type.Name == "MainTabWindow_WorkTab"
                    select type).Single();
                patchClass = (from asm in AppDomain.CurrentDomain.GetAssemblies()
                    from type in asm.GetTypes()
                    where type.IsClass && type.Name == "WorkTab_AddButtonToFluffysWorktab"
                    select type).Single();
                DrawUtil.MaxPriority = 9;
            }

            var worktabDoContents = AccessTools.Method(worktab, "DoWindowContents");
            var patchPostfix = AccessTools.Method(patchClass, "Postfix");
            HarmonyInst.Patch(worktabDoContents, postfix: new HarmonyMethod(patchPostfix));
        }

        public static SettingHandle<double> PassionMult { get; private set; }

        public override void DefsLoaded()
        {
            base.DefsLoaded();
            PassionMult = Settings.GetHandle("passionMult", "Passion multiplier",
                "Determines the importance of passions whe assigning priorities", 1d);
        }
    }
}