using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using AutoPriorities.ImportantJobs;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.PawnDataSerializer.Exporter;
using AutoPriorities.Ui;
using AutoPriorities.Utils.Extensions;
using AutoPriorities.WorldInfoRetriever;
using HarmonyLib;
using Verse;
using ILogger = AutoPriorities.APLogger.ILogger;
using Logger = AutoPriorities.APLogger.Logger;

namespace AutoPriorities.Core
{
    [StaticConstructorOnStartup]
    public static class Controller
    {
        public static ILogger? logger;
        private static PawnsData? _pawnData;
        private static PawnsDataBuilder? _pawnsDataBuilder;
        private static Harmony? _harmony;
        private static ModContentPack? _contentPack;
        private static int _nextSetPrioritiesTick = -1;

        private static readonly ConcurrentQueue<Action> DelayedActionsQueue = new();

        static Controller()
        {
            var mod = LoadedModManager.GetMod<AutoPrioritiesMod>();
            var contentPack = mod?.Content;
            if (contentPack == null)
            {
                Log.Warning("AutoPriorities: ModContentPack not found; skipping initialization.");
                return;
            }

            Initialize(contentPack);
        }

        public static AutoPrioritiesDialog? Dialog { get; private set; }
        public static MapSpecificData? AbandonedMapMapSpecificData { get; set; }

        public static int? MaxPriorityAlien { get; set; }

        public static int MaxPriority => AutoPrioritiesMod.Settings?.maxPriority ?? 4;
        public static bool UseOldAssignmentAlgorithm => AutoPrioritiesMod.Settings?.useOldAssignmentAlgorithm ?? false;
        public static bool DebugSaveTablesAndPawns => AutoPrioritiesMod.Settings?.debugSaveTablesAndPawns ?? false;
        public static bool DebugLogs => AutoPrioritiesMod.Settings?.debugLogs ?? false;
        public static bool AnnonyingDebugLogs => AutoPrioritiesMod.Settings?.annonyingDebugLogs ?? false;

        public static float OptimizationFeasibleSolutionTimeoutSeconds =>
            AutoPrioritiesMod.Settings?.optimizationFeasibleSolutionTimeoutSeconds ?? 10f;

        public static float OptimizationImprovementSeconds => AutoPrioritiesMod.Settings?.optimizationImprovementSeconds ?? 1f;
        public static float OptimizationMutationRate => AutoPrioritiesMod.Settings?.optimizationMutationRate ?? 0.8f;
        public static int OptimizationPopulationSize => AutoPrioritiesMod.Settings?.optimizationPopulationSize ?? 256;
        public static float OptimizationJobsPerPawnWeight => AutoPrioritiesMod.Settings?.optimizationJobsPerPawnWeight ?? 1f;
        public static int TimerTicks => AutoPrioritiesMod.Settings?.timerTicks ?? 60000;
        public static event Action? SetPrioritiesOnTimerCallback;

        public static void Initialize(ModContentPack contentPack)
        {
            if (_harmony != null)
                return;

            _contentPack = contentPack;
            logger = new Logger();

            _harmony = new Harmony("autoPriorities");

            PatchMod("fluffy.worktab", "FluffyWorktabPatch.dll");
            PatchMod("arof.fluffy.worktab", "FluffyWorktabPatch.dll");
            PatchMod("arof.fluffy.worktab.continued", "FluffyWorktabPatch.dll");
            PatchMod("voult.betterpawncontrol", "BetterPawnControlPatch.dll");

            _harmony.PatchAll();
        }

        public static void OnGameLoaded()
        {
            LongEventHandler.ExecuteWhenFinished(() =>
                {
                    if (Find.CurrentMap == null)
                        return;

                    if (Dialog == null)
                        Dialog = CreateDialog();

                    SetupPrioritiesOnTimerIfNeeded();
                }
            );
        }

        public static void GameTick()
        {
            if (_nextSetPrioritiesTick > 0 && Find.TickManager.TicksGame >= _nextSetPrioritiesTick)
                SetPriorities();
        }

        public static void ProcessDelayedActions()
        {
            while (DelayedActionsQueue.TryDequeue(out var action))
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    logger?.Err($"Exception thrown while executing action: {e}");
                }
        }

        public static void EnqueueDelayedAction(Action action)
        {
            DelayedActionsQueue.Enqueue(action);
        }

        public static void SwitchMap()
        {
            if (Find.CurrentMap == null)
                return;

            LongEventHandler.ExecuteWhenFinished(() =>
                {
                    if (Find.CurrentMap == null)
                        return;

                    if (_pawnData == null || _pawnsDataBuilder == null)
                        Dialog = CreateDialog();
                    else
                        _pawnsDataBuilder.Build(_pawnData);

                    SetupPrioritiesOnTimerIfNeeded();
                }
            );
        }

        public static void RebuildPawns()
        {
            _pawnData?.Rebuild();
        }

        public static void SetupPrioritiesOnTimerIfNeeded()
        {
            _nextSetPrioritiesTick = -1;

            if (_pawnData?.RunOnTimer != true)
                return;

            if (Find.TickManager == null)
                return;

            if (DebugLogs)
                logger?.Info($"Set up set priorities to run every {TimerTicks} ticks");

            var timerTicks = TimerTicks;
            if (timerTicks <= 0)
                return;

            _nextSetPrioritiesTick = Find.TickManager.TicksGame + timerTicks;
        }

        private static void SetPriorities()
        {
            if (DebugLogs)
                logger?.Info("Auto running priorities on timer...");

            if (_pawnData?.RunOnTimer != true)
                return;

            if (Find.TickManager == null)
                return;

            Dialog?.RunSetPriorities(() => SetPrioritiesOnTimerCallback?.Invoke());

            var timerTicks = TimerTicks;
            if (timerTicks <= 0)
            {
                _nextSetPrioritiesTick = -1;
                return;
            }

            _nextSetPrioritiesTick = Find.TickManager.TicksGame + timerTicks;
        }

        private static void PatchMod(string packageId, string patchName)
        {
            if (!LoadedModManager.RunningModsListForReading.Exists(m => m.PackageId == packageId))
                return;

            if (_contentPack == null || _harmony == null)
                return;

            logger?.Info($"Patching for: {packageId}");

            var asm = Assembly.LoadFile(Path.Combine(_contentPack.RootDir, Path.Combine("ConditionalAssemblies/1.6/", patchName)));
            _harmony.PatchAll(asm);

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
            var saveDataHandler = new SaveDataHandler(stringSerializer);
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
