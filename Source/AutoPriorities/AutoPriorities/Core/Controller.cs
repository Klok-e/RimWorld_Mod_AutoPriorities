using System;
using System.IO;
using System.Reflection;
using AutoPriorities.Extensions;
using AutoPriorities.ImportantJobs;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.PawnDataSerializer.Exporter;
using AutoPriorities.Ui;
using AutoPriorities.WorldInfoRetriever;
using HugsLib;
using HugsLib.Settings;
using Verse;
using ILogger = AutoPriorities.APLogger.ILogger;
using Logger = AutoPriorities.APLogger.Logger;

namespace AutoPriorities.Core
{
    public class Controller : ModBase
    {
        public static ILogger? logger;
        private static PawnsData? _pawnData;
        private static PawnsDataBuilder? _pawnsDataBuilder;

        public static SettingHandle<double>? MinimumSkill { get; private set; }
        
        public static AutoPrioritiesDialog? Dialog { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            logger = new Logger(Logger);

            PatchMod("fluffy.worktab", "FluffyWorktabPatch.dll");
            HarmonyInst.PatchAll();
        }

        public override void MapLoaded(Map map)
        {
            base.WorldLoaded();
            Dialog = CreateDialog();
        }
        
        public override void DefsLoaded()
        {
            base.DefsLoaded();
            MinimumSkill = Settings.GetHandle(
                "minimumFitness",
                "Minimum fitness",
                "Determines whether the pawn is eligible for the work type. " +
                "If minimumFitness < skill * learnRate, work type isn't assigned",
                0d);
        }

        public static void SwitchMap()
        {
            if (_pawnData == null) return;

            _pawnsDataBuilder?.Build(_pawnData);
        }

        public static void RebuildPawns()
        {
            _pawnData?.Rebuild();
        }

        private void PatchMod(string packageId, string patchName)
        {
            if (!LoadedModManager.RunningModsListForReading.Exists(m => m.PackageId == packageId)) return;

            logger?.Info($"Patching for: {packageId}");

            var asm = Assembly.LoadFile(
                Path.Combine(ModContentPack.RootDir, Path.Combine("ConditionalAssemblies/1.4/", patchName)));
            HarmonyInst.PatchAll(asm);

            var methods = asm.GetMethodsWithHelpAttribute<PatchInitializeAttribute>();
            foreach (var method in methods) method.Invoke(null, null);
        }

        private static string GetSaveLocation()
        {
            // Get method "FolderUnderSaveData" from GenFilePaths, which is private (NonPublic) and static.
            var method = typeof(GenFilePaths).GetMethod(
                "FolderUnderSaveData",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new Exception("AutoPriorities :: FolderUnderSaveData is null [reflection]");

            // Call "FolderUnderSaveData" from null parameter, since this is a static method.
            return (string)method.Invoke(null, new object[] { "PrioritiesData" });
        }

        private static AutoPrioritiesDialog CreateDialog()
        {
            var savePath = GetSaveLocation();

            var worldInfo = new WorldInfoRetriever.WorldInfoRetriever();
            var log = logger!;
            var worldFacade = new WorldInfoFacade(worldInfo, log);
            var stringSerializer = new PawnDataStringSerializer(log, worldFacade);
            var mapSpecificSerializer = new MapSpecificDataPawnsDataSerializer(worldInfo, stringSerializer);
            _pawnsDataBuilder = new PawnsDataBuilder(mapSpecificSerializer, worldInfo, log);
            _pawnData = _pawnsDataBuilder.Build();
            var importantWorkTypes = new ImportantJobsProvider(worldFacade);
            var priorityAssigner = new PrioritiesAssigner(_pawnData, log, importantWorkTypes, worldInfo);
            var pawnDataExporter = new PawnDataExporter(log, savePath, _pawnData, stringSerializer);

            return new AutoPrioritiesDialog(_pawnData, priorityAssigner, log, importantWorkTypes, pawnDataExporter);
        }
    }
}
