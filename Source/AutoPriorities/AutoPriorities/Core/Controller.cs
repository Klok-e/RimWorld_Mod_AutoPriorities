using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using System.Reflection;
using AutoPriorities.HarmonyPatches;
using AutoPriorities.Percents;
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
        public static ModLogger? Log { get; private set; }

        private static AutoPriorities_Dialog? _dialog;
        public static AutoPriorities_Dialog Dialog => _dialog ??= new AutoPriorities_Dialog();

        public static PoolFactory<Number, NumberPoolArgs> PoolNumbers { get; } =
            new PoolFactory<Number, NumberPoolArgs>();

        public static PoolFactory<Percent, PercentPoolArgs> PoolPercents { get; } =
            new PoolFactory<Percent, PercentPoolArgs>();

        public override void Initialize()
        {
            base.Initialize();
            Log = Logger;
            if (PatchMod("fluffy.worktab", "FluffyWorktabPatch.dll"))
                DrawUtil.MaxPriority = 9;
            PatchMod("dame.interestsframework", "InterestsPatch.dll");
        }

        private bool PatchMod(string packageId, string patchName)
        {
            if (LoadedModManager.RunningModsListForReading.Exists(m => m.PackageId == packageId))
            {
#if DEBUG
                Log!.Message($"{packageId} detected");
#endif

                var asm = Assembly.LoadFile(Path.Combine(ModContentPack.RootDir,
                    Path.Combine("ConditionalAssemblies/1.1/", patchName)));

                HarmonyInst.PatchAll(asm);
                return true;
            }
#if DEBUG
            Log!.Message($"No {packageId} detected");
#endif
            return false;
        }

        public static SettingHandle<double>? PassionMult { get; private set; }

        public override void DefsLoaded()
        {
            base.DefsLoaded();
            PassionMult = Settings.GetHandle("passionMult", "Passion multiplier",
                "Determines the importance of passions when assigning priorities", 1d);
        }
    }
}