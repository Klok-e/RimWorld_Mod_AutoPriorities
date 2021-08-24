using System;
using System.IO;
using System.Reflection;
using AutoPriorities.Extensions;
using AutoPriorities.ImportantJobs;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.PawnDataSerializer.Exporter;
using AutoPriorities.PawnDataSerializer.StreamProviders;
using AutoPriorities.Ui;
using AutoPriorities.WorldInfoRetriever;
using HugsLib;
using HugsLib.Settings;
using UnityEngine;
using Verse;
using ILogger = AutoPriorities.APLogger.ILogger;
using Logger = AutoPriorities.APLogger.Logger;

namespace AutoPriorities.Core
{
    public class Controller : ModBase
    {
        private static ILogger? _logger;

        public static AutoPrioritiesDialog? Dialog { get; private set; }

        public static SettingHandle<double>? PassionMult { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            _logger = new Logger(Logger);

            PatchMod("fluffy.worktab", "FluffyWorktabPatch.dll");
            PatchMod("dame.interestsframework", "InterestsPatch.dll");
            HarmonyInst.PatchAll();
        }

        public override void MapLoaded(Map map)
        {
            base.MapLoaded(map);
            Dialog = CreateDialog();
        }

        public override void DefsLoaded()
        {
            base.DefsLoaded();
            PassionMult = Settings.GetHandle(
                "passionMult",
                "Passion multiplier",
                "Determines the importance of passions when assigning priorities",
                1d);
        }

        private void PatchMod(string packageId, string patchName)
        {
            if (!LoadedModManager.RunningModsListForReading.Exists(m => m.PackageId == packageId)) return;

            _logger?.Info($"Patching for: {packageId}");

            var asm = Assembly.LoadFile(
                Path.Combine(ModContentPack.RootDir, Path.Combine("ConditionalAssemblies/1.3/", patchName)));
            HarmonyInst.PatchAll(asm);

            var methods = asm.GetMethodsWithHelpAttribute<PatchInitializeAttribute>();
            foreach (var method in methods) method.Invoke(null, null);
        }

        private static string GetSaveLocation()
        {
            // Get method "FolderUnderSaveData" from GenFilePaths, which is private (NonPublic) and static.
            var folder = typeof(GenFilePaths).GetMethod(
                "FolderUnderSaveData",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (folder == null) throw new Exception("AutoPriorities :: FolderUnderSaveData is null [reflection]");

            // Call "FolderUnderSaveData" from null parameter, since this is a static method.
            return (string)folder.Invoke(null, new object[] { "PrioritiesData" });
        }

        private static AutoPrioritiesDialog CreateDialog()
        {
            const string filename = "ModAutoPrioritiesSaveNEW.xml";
            string fullPath = Application.persistentDataPath + filename;
            var savePath = GetSaveLocation();

            var worldInfo = new WorldInfoRetriever.WorldInfoRetriever();
            var logger = _logger!;
            var worldFacade = new WorldInfoFacade(worldInfo, logger);
            var streamProvider = new FileStreamProvider();
            var stringSerializer = new PawnDataStringSerializer(logger, worldFacade);
            var mapSpecificSerializer = new MapSpecificDataPawnsDataSerializer(logger, worldFacade, stringSerializer);
            var serializer = new PawnsDataSerializer(logger, fullPath, worldFacade, streamProvider);
            var combinedSer = new CombinedPawnsDataSerializer(logger, mapSpecificSerializer, serializer);
            var pawnData = new PawnsDataBuilder(combinedSer, worldInfo, logger).Build();
            var importantWorkTypes = new ImportantJobsProvider(worldFacade);
            var priorityAssigner = new PrioritiesAssigner(pawnData, logger, importantWorkTypes);
            var pawnDataExporter = new PawnDataExporter(logger, savePath, pawnData, stringSerializer);

            return new AutoPrioritiesDialog(pawnData, priorityAssigner, logger, importantWorkTypes, pawnDataExporter);
        }
    }
}
