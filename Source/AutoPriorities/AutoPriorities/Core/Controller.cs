using System.IO;
using System.Reflection;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.PawnDataSerializer.StreamProviders;
using AutoPriorities.Percents;
using AutoPriorities.Utils;
using AutoPriorities.WorldInfoRetriever;
using HugsLib;
using HugsLib.Settings;
using HugsLib.Utils;
using UnityEngine;
using Verse;
using Logger = AutoPriorities.APLogger.Logger;

namespace AutoPriorities.Core
{
    public class Controller : ModBase
    {
        private static AutoPrioritiesDialog? _dialog;

        public static ModLogger? Log { get; private set; }

        public static AutoPrioritiesDialog Dialog => _dialog ??= CreateDialog();

        public static PoolFactory<Number, NumberPoolArgs> PoolNumbers { get; } =
            new();

        public static PoolFactory<Percent, PercentPoolArgs> PoolPercents { get; } =
            new();

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

        private static AutoPrioritiesDialog CreateDialog()
        {
            const string filename = "ModAutoPrioritiesSaveNEW.xml";
            string fullPath = Application.persistentDataPath + filename;

            var worldInfo = new WorldInfoRetriever.WorldInfoRetriever();
            var logger = new Logger();
            var worldFacade = new WorldInfoFacade(worldInfo, logger);
            var streamProvider = new FileStreamProvider();
            var serializer = new PawnsDataSerializer(logger, fullPath, worldFacade, streamProvider);
            var pawnData = new PawnsData(serializer, worldInfo);
            var priorityAssigner = new PrioritiesAssigner(worldFacade);

            return new AutoPrioritiesDialog(pawnData, priorityAssigner);
        }
    }
}
