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
using UnityEngine;
using Verse;
using ILogger = AutoPriorities.APLogger.ILogger;
using Logger = AutoPriorities.APLogger.Logger;

namespace AutoPriorities.Core
{
    public class Controller : ModBase
    {
        public static ILogger? logger;
        public static PawnsData? pawnData;

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
            base.MapLoaded(map);
            Dialog = CreateDialog();
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
            var log = Controller.logger!;
            var worldFacade = new WorldInfoFacade(worldInfo, log);
            var stringSerializer = new PawnDataStringSerializer(log, worldFacade);
            var mapSpecificSerializer = new MapSpecificDataPawnsDataSerializer(worldInfo, stringSerializer);
            pawnData = new PawnsDataBuilder(mapSpecificSerializer, worldInfo, log).Build();
            var importantWorkTypes = new ImportantJobsProvider(worldFacade);
            var priorityAssigner = new PrioritiesAssigner(pawnData, log, importantWorkTypes);
            var pawnDataExporter = new PawnDataExporter(log, savePath, pawnData, stringSerializer);

            return new AutoPrioritiesDialog(pawnData, priorityAssigner, log, importantWorkTypes, pawnDataExporter);
        }
    }
}
