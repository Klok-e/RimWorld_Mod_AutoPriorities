using System;
using System.IO;
using System.Reflection;
using AutoPriorities.ImportantJobs;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.PawnDataSerializer.Exporter;
using AutoPriorities.Ui;
using AutoPriorities.Utils.Extensions;
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

        public static SettingHandle<int>? MaxPriority { get; private set; }

        public static SettingHandle<float>? OptimizationFeasibleSolutionTimeoutSeconds { get; private set; }

        public static SettingHandle<float>? OptimizationImprovementSeconds { get; private set; }

        public static SettingHandle<float>? OptimizationMutationRate { get; private set; }

        public static SettingHandle<int>? OptimizationPopulationSize { get; private set; }

        public static SettingHandle<bool>? UseOldAssignmentAlgorithm { get; private set; }

        public static SettingHandle<bool>? DebugSaveTablesAndPawns { get; private set; }

        public static SettingHandle<bool>? DebugLogs { get; private set; }

        public static SettingHandle<float>? OptimizationJobsPerPawnWeight { get; private set; }

        public static SettingHandle<int>? TimerTicks { get; private set; }

        public static int? MaxPriorityAlien { get; set; }

        public static AutoPrioritiesDialog? Dialog { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            logger = new Logger(Logger);

            PatchMod("fluffy.worktab", "FluffyWorktabPatch.dll");
            PatchMod("arof.fluffy.worktab", "FluffyWorktabPatch.dll");

            HarmonyInst.PatchAll();
        }

        public override void MapLoaded(Map map)
        {
            base.WorldLoaded();
            Dialog = CreateDialog();

            SetupPrioritiesOnTimerIfNeeded();
        }

        public override void DefsLoaded()
        {
            base.DefsLoaded();
            MaxPriority = Settings.GetHandle("maxPriority", "Max priority", "Sets max priority", 4);
            UseOldAssignmentAlgorithm = Settings.GetHandle(
                "useOldAssignmentAlgorithm",
                "Use old assignment algorithm",
                "Use the old greedy assignment algorithm; the new algorithm represents "
                + "assignment problem as a linear programming optimization problem and uses an LP solver to get an optimal solution.",
                false
            );

            DebugSaveTablesAndPawns = Settings.GetHandle(
                "debugSaveTablesAndPawns",
                "Debug save tables and pawns",
                "Debug save tables and pawns",
                false
            );
            DebugLogs = Settings.GetHandle("debugLogs", "Debug logs", "Debug logs", false);
            OptimizationFeasibleSolutionTimeoutSeconds = Settings.GetHandle(
                "optimizationFeasibleSolutionTimeoutSeconds",
                "Optimization feasible solution timeout",
                "For how long to wait before abandoning finding a solution which satisfies all restrictions (random search).",
                10f,
                x => float.TryParse(x, out var result) && result is >= 0f and <= 120
            );
            OptimizationImprovementSeconds = Settings.GetHandle(
                "optimizationImprovementSeconds",
                "Optimization improvement seconds",
                "For how long to try to optimize the solution after finding a solution which satisfies all restrictions. "
                + "Increase to increase likelihood of an optimal or a more consistent solution.",
                1f,
                x => float.TryParse(x, out var result) && result is >= 0f and <= 60
            );
            OptimizationMutationRate = Settings.GetHandle(
                "optimizationMutationRate",
                "Optimization mutation rate",
                "The rate at which mutations occur during the optimization process. Parameter of random search.",
                0.8f,
                x => float.TryParse(x, out var result) && result is >= 0f and <= 1f
            );
            OptimizationPopulationSize = Settings.GetHandle(
                "optimizationPopulationSize",
                "Optimization population size",
                "The population size used in the optimization algorithm. Reduce to reduce memory footprint.",
                256,
                x => int.TryParse(x, out var result) && result is >= 2 and <= 4096
            );
            OptimizationJobsPerPawnWeight = Settings.GetHandle(
                "optimizationJobsPerPawnWeight",
                "Optimization jobs per pawn weight",
                "Controls spread of jobs over multiple pawns. Applies only for variables which weren't found with a continuous LP solver. Very minor impact.",
                1f,
                x => float.TryParse(x, out var result) && result >= 0
            );

            TimerTicks = Settings.GetHandle(
                "timerTicks",
                "Timer ticks",
                "Used in a timer for setting priorities periodically. Default - 24 hours (60000 ticks).",
                60000,
                x => int.TryParse(x, out var result) && result > 0
            );
            TimerTicks.ValueChanged += _ => { SetupPrioritiesOnTimerIfNeeded(); };
        }

        public static void SwitchMap()
        {
            if (_pawnData == null)
                return;

            _pawnsDataBuilder?.Build(_pawnData);

            SetupPrioritiesOnTimerIfNeeded();
        }

        public static void RebuildPawns()
        {
            _pawnData?.Rebuild();
        }

        public static void SetupPrioritiesOnTimerIfNeeded()
        {
            HugsLibController.Instance.TickDelayScheduler.TryUnscheduleCallback(SetPriorities);

            if (_pawnData?.RunOncePerDay != true)
                return;

            if (DebugLogs)
                logger?.Info($"Set up set priorities to run every {TimerTicks} ticks");

            HugsLibController.Instance.TickDelayScheduler.ScheduleCallback(SetPriorities, TimerTicks, repeat: true);
        }

        private static void SetPriorities()
        {
            if (DebugLogs)
                logger?.Info("Auto running priorities on timer...");

            Dialog?.RunSetPriorities();
        }

        private void PatchMod(string packageId, string patchName)
        {
            if (!LoadedModManager.RunningModsListForReading.Exists(m => m.PackageId == packageId))
                return;

            logger?.Info($"Patching for: {packageId}");

            var asm = Assembly.LoadFile(Path.Combine(ModContentPack.RootDir, Path.Combine("ConditionalAssemblies/1.5/", patchName)));
            HarmonyInst.PatchAll(asm);

            var methods = asm.GetMethodsWithHelpAttribute<PatchInitializeAttribute>();
            foreach (var method in methods)
                method.Invoke(null, null);
        }

        private static string GetSaveLocation()
        {
            // Get method "FolderUnderSaveData" from GenFilePaths, which is private (NonPublic) and static.
            var method = typeof(GenFilePaths).GetMethod("FolderUnderSaveData", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new Exception("AutoPriorities :: FolderUnderSaveData is null [reflection]");

            // Call "FolderUnderSaveData" from null parameter, since this is a static method.
            return (string)method.Invoke(null, new object[] { "PrioritiesData" });
        }

        private static AutoPrioritiesDialog CreateDialog()
        {
            var savePath = GetSaveLocation();

            var worldInfoRetriever = new WorldInfoRetriever.WorldInfoRetriever();
            var log = logger!;
            var worldFacade = new WorldInfoFacade(worldInfoRetriever, log);
            var stringSerializer = new PawnDataStringSerializer(log, worldFacade);
            var saveDataHandler = new SaveDataHandler(log, stringSerializer);
            var mapSpecificSerializer = new MapSpecificDataPawnsDataSerializer(log, stringSerializer, saveDataHandler);
            var workSpeedCalculator = new WorkSpeedCalculator(log, worldInfoRetriever);
            _pawnsDataBuilder = new PawnsDataBuilder(mapSpecificSerializer, worldInfoRetriever, log, workSpeedCalculator);
            _pawnData = _pawnsDataBuilder.Build();
            var importantWorkTypes = new ImportantJobsProvider(worldFacade);
            var priorityAssigner = new PrioritiesAssigner(_pawnData, log, importantWorkTypes, worldInfoRetriever);
            var saveFilePather = new SaveFilePather(savePath);
            var pawnDataExporter = new PawnDataExporter(log, savePath, _pawnData, saveFilePather, stringSerializer, saveDataHandler);

            return new AutoPrioritiesDialog(_pawnData, priorityAssigner, log, importantWorkTypes, pawnDataExporter, worldInfoRetriever);
        }
    }
}
