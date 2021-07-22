using System.IO;
using System.Reflection;
using AutoPriorities.Percents;
using AutoPriorities.Utils;
using HugsLib;
using HugsLib.Settings;
using HugsLib.Utils;
using Verse;

namespace AutoPriorities.Core
{
    public class Controller : ModBase
    {
        private static AutoPriorities_Dialog? _dialog;

        public static ModLogger? Log { get; private set; }

        public static AutoPriorities_Dialog Dialog => _dialog ??= new AutoPriorities_Dialog();

        public static PoolFactory<Number, NumberPoolArgs> PoolNumbers { get; } =
            new PoolFactory<Number, NumberPoolArgs>();

        public static PoolFactory<Percent, PercentPoolArgs> PoolPercents { get; } =
            new PoolFactory<Percent, PercentPoolArgs>();

        public static SettingHandle<double>? PassionMult { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            Log = Logger;
            if (PatchMod("fluffy.worktab", "FluffyWorktabPatch.dll")) DrawUtil.MaxPriority = 9;
            PatchMod("dame.interestsframework", "InterestsPatch.dll");
            HarmonyInst.PatchAll();
        }

        public override void DefsLoaded()
        {
            base.DefsLoaded();
            PassionMult = Settings.GetHandle("passionMult", "Passion multiplier",
                "Determines the importance of passions when assigning priorities", 1d);
        }

        private bool PatchMod(string packageId, string patchName)
        {
            if (LoadedModManager.RunningModsListForReading.Exists(m => m.PackageId == packageId))
            {
#if DEBUG
                Log!.Message($"{packageId} detected");
#endif

                var asm = Assembly.LoadFile(Path.Combine(ModContentPack.RootDir,
                    Path.Combine("ConditionalAssemblies/1.3/", patchName)));

                HarmonyInst.PatchAll(asm);
                return true;
            }
#if DEBUG
            Log!.Message($"No {packageId} detected");
#endif
            return false;
        }
    }
}
