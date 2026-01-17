using System;
using System.Collections.Generic;
using System.Linq;
using AutoPriorities.APLogger;
using AutoPriorities.Core;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.Percents;
using AutoPriorities.Utils.Extensions;
using AutoPriorities.WorldInfoRetriever;
using AutoPriorities.Wrappers;

namespace AutoPriorities
{
    public class PawnsData
    {
        private readonly ILogger _logger;
        private readonly IPawnsDataSerializer _serializer;
        private readonly IWorkSpeedCalculator _workSpeedCalculator;
        private readonly IWorldInfoRetriever _worldInfoRetriever;

        public PawnsData(IPawnsDataSerializer serializer, IWorldInfoRetriever worldInfoRetriever, ILogger logger,
            IWorkSpeedCalculator workSpeedCalculator)
        {
            _serializer = serializer;
            _worldInfoRetriever = worldInfoRetriever;
            _logger = logger;
            _workSpeedCalculator = workSpeedCalculator;
        }

        public List<WorkTableEntry> WorkTables { get; private set; } = new();

        public HashSet<ExcludedPawnEntry> ExcludedPawns { get; private set; } = new();

        public HashSet<IWorkTypeWrapper> WorkTypes { get; } = new();

        public HashSet<IWorkTypeWrapper> WorkTypesNotRequiringSkills { get; private set; } = new();

        public Dictionary<IWorkTypeWrapper, List<PawnFitnessData>> SortedPawnFitnessForEveryWork { get; private set; } = new();

        public List<IPawnWrapper> CurrentMapPlayerPawns { get; private set; } = new();

        public List<IPawnWrapper> AllPlayerPawns { get; private set; } = new();

        public bool IgnoreLearningRate { get; set; }

        public bool IgnoreWorkSpeed { get; set; }

        public bool IgnoreOppositionToWork { get; set; }

        public bool IgnoreDownedStatus { get; set; }

        public bool RunOnTimer { get; set; }

        public float MinimumSkillLevel { get; set; }

        public PawnsData ShallowCopy()
        {
            var shallowCopy =
                new PawnsData(_serializer, _worldInfoRetriever, _logger, _workSpeedCalculator)
                {
                    WorkTables = WorkTables.Select(x => x.ShallowCopy()).ToList(),
                    ExcludedPawns = ExcludedPawns.ToHashSet(),
                    WorkTypesNotRequiringSkills = WorkTypesNotRequiringSkills.ToHashSet(),
                    SortedPawnFitnessForEveryWork = SortedPawnFitnessForEveryWork.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    CurrentMapPlayerPawns = CurrentMapPlayerPawns.ToList(),
                    AllPlayerPawns = AllPlayerPawns.ToList(),
                    IgnoreLearningRate = IgnoreLearningRate,
                    IgnoreOppositionToWork = IgnoreOppositionToWork,
                    IgnoreDownedStatus = IgnoreDownedStatus,
                    MinimumSkillLevel = MinimumSkillLevel,
                    IgnoreWorkSpeed = IgnoreWorkSpeed,
                    RunOnTimer = RunOnTimer,
                };

            return shallowCopy;
        }

        public void SetData(SaveData data)
        {
            // Excluded must be loaded first because State depends on ExcludedPawns being filled
            ExcludedPawns = data.ExcludedPawns;
            IgnoreLearningRate = data.IgnoreLearningRate;
            MinimumSkillLevel = data.MinimumSkillLevel;
            IgnoreOppositionToWork = data.IgnoreOppositionToWork;
            IgnoreDownedStatus = data.IgnoreDownedStatus;
            IgnoreWorkSpeed = data.IgnoreWorkSpeed;
            RunOnTimer = data.RunOnTimer;

            WorkTables = LoadSavedState(data.WorkTablesData);

            if (_worldInfoRetriever.DebugLogs())
            {
                _logger.Info(
                    $"[SetData] first job count {WorkTables.FirstOrDefault().JobCount.v}; "
                    + $"load job count: {data.WorkTablesData.FirstOrDefault().JobCount.v}"
                );
            }
        }

        public void SaveState()
        {
            try
            {
                _serializer.SaveData(GetSaveDataRequest());
            }
            catch (Exception e)
            {
                _logger.Err(e);
            }
        }

        public SaveDataRequest GetSaveDataRequest()
        {
            return new SaveDataRequest
            {
                ExcludedPawns = ExcludedPawns,
                WorkTablesData = WorkTables,
                IgnoreLearningRate = IgnoreLearningRate,
                MinimumSkillLevel = MinimumSkillLevel,
                IgnoreOppositionToWork = IgnoreOppositionToWork,
                IgnoreDownedStatus = IgnoreDownedStatus,
                IgnoreWorkSpeed = IgnoreWorkSpeed,
                RunOnTimer = RunOnTimer,
            };
        }

        public void Rebuild()
        {
            try
            {
                // get all work types
                var workTypes = _worldInfoRetriever.GetWorkTypeDefsInPriorityOrder().ToArray();

                if (_worldInfoRetriever.AnnoyingDebugLogs())
                    _logger.Info($"workTypes.Length {workTypes.Length}");

                var allPawns = _worldInfoRetriever.GetAllAdultPawnsInPlayerFaction().ToList();

                if (_worldInfoRetriever.AnnoyingDebugLogs())
                    _logger.Info($"allPawns.Count {allPawns.Count}");

                AllPlayerPawns.Clear();
                AllPlayerPawns.AddRange(allPawns);

                // get all pawns owned by player
                var pawns = _worldInfoRetriever.GetAdultPawnsInPlayerFactionInCurrentMap();
                if (pawns == null)
                {
                    if (_worldInfoRetriever.DebugLogs())
                        _logger.Info("No map currently loaded. Skipping pawn data rebuild.");

                    return;
                }

                if (_worldInfoRetriever.AnnoyingDebugLogs())
                    _logger.Info($"pawns.Count {allPawns.Count}");

                // get all skills associated with the work types
                CurrentMapPlayerPawns.Clear();
                CurrentMapPlayerPawns.AddRange(pawns);

                SortedPawnFitnessForEveryWork.Clear();
                foreach (var work in workTypes)
                {
                    SortedPawnFitnessForEveryWork[work] = new List<PawnFitnessData>();
                    foreach (var pawn in CurrentMapPlayerPawns)
                        try
                        {
                            if (!pawn.IsCapableOfWholeWorkType(work)
                                || (pawn.IsIncapacitated() && !IgnoreDownedStatus)
                                || ExcludedPawns.Contains(new ExcludedPawnEntry { WorkDef = work, Pawn = pawn }))
                            {
                                continue;
                            }

                            var skill = pawn.AverageOfRelevantSkillsFor(work);
                            var learningRateFactor = IgnoreLearningRate ? 1 : pawn.MaxLearningRateFactor(work);
                            var averageWorkSpeed = IgnoreWorkSpeed ? 1 : _workSpeedCalculator.AverageWorkSpeed(pawn, work);

                            var isSkilledWorkType = work.RelevantSkillsCount > 0;
                            var fitness = (isSkilledWorkType ? skill * learningRateFactor : 0.001f) * averageWorkSpeed;

                            SortedPawnFitnessForEveryWork[work]
                                .Add(
                                    new PawnFitnessData
                                    {
                                        Fitness = fitness,
                                        Pawn = pawn,
                                        SkillLevel = skill,
                                        IsOpposed = pawn.IsOpposedToWorkType(work),
                                        IsSkilledWorkType = isSkilledWorkType,
                                    }
                                );
                        }
                        catch (Exception e)
                        {
                            _logger.Err($"error: {e} for pawn {pawn.NameFullColored}");
                            _logger.Err(e);
                        }

                    if (WorkTypes.Contains(work))
                        continue;

                    WorkTypes.Add(work);

                    if (work.RelevantSkillsCount == 0)
                        WorkTypesNotRequiringSkills.Add(work);
                }

                foreach (var keyValue in SortedPawnFitnessForEveryWork)
                    keyValue.Value.Sort((x, y) => y.Fitness.CompareTo(x.Fitness));
            }
            catch (Exception e)
            {
                _logger.Err("An error occured when rebuilding PawnData:");
                _logger.Err(e);
            }
        }

        public bool SeedFromWorkTab(bool force)
        {
            if (!force && WorkTables.Count != 0)
                return false;

            if (CurrentMapPlayerPawns.Count == 0 || WorkTypes.Count == 0)
                return false;

            var workTypes = WorkTypes.ToList();
            var workTables = new List<WorkTableEntry>();

            var maxAssignedPriority = 0;
            foreach (var workType in workTypes)
            {
                if (!SortedPawnFitnessForEveryWork.TryGetValue(workType, out var fitnessData))
                    continue;

                maxAssignedPriority =
                    fitnessData.Where(CanPawnBeAssigned)
                        .Select(x => x.Pawn)
                        .Select(pawn => pawn.WorkSettingsGetPriority(workType))
                        .Prepend(maxAssignedPriority)
                        .Max();
            }

            if (maxAssignedPriority <= 0)
                return false;

            for (var priority = 1; priority <= maxAssignedPriority; priority++)
            {
                var workTypePercents = new Dictionary<IWorkTypeWrapper, TablePercent>();
                foreach (var workType in workTypes)
                {
                    if (!SortedPawnFitnessForEveryWork.TryGetValue(workType, out var fitnessData))
                    {
                        workTypePercents[workType] = TablePercent.Percent(0);
                        continue;
                    }

                    var eligibleCount = 0;
                    var priorityCount = 0;
                    foreach (var pawn in fitnessData.Where(CanPawnBeAssigned).Select(x => x.Pawn))
                    {
                        eligibleCount++;
                        if (pawn.WorkSettingsGetPriority(workType) == priority)
                            priorityCount++;
                    }

                    var percent = eligibleCount > 0 ? (double)priorityCount / eligibleCount : 0d;
                    workTypePercents[workType] = TablePercent.Percent(percent);
                }

                var maxJobsPerPawn = 0;
                var jobsPerPawn = new Dictionary<IPawnWrapper, int>();
                foreach (var workType in workTypes)
                {
                    if (!SortedPawnFitnessForEveryWork.TryGetValue(workType, out var fitnessData))
                        continue;

                    foreach (var pawnFitness in fitnessData.Where(CanPawnBeAssigned))
                    {
                        var pawn = pawnFitness.Pawn;
                        if (pawn.WorkSettingsGetPriority(workType) != priority)
                            continue;

                        if (jobsPerPawn.TryGetValue(pawn, out var count))
                            jobsPerPawn[pawn] = count + 1;
                        else
                            jobsPerPawn[pawn] = 1;
                    }
                }

                if (jobsPerPawn.Count > 0)
                    maxJobsPerPawn = jobsPerPawn.Values.Max();

                workTables.Add(new WorkTableEntry { Priority = priority, JobCount = maxJobsPerPawn, WorkTypes = workTypePercents });
            }

            WorkTables = workTables;
            return true;
        }

        public bool SeedFromWorkTabIfEmpty()
        {
            return SeedFromWorkTab(false);
        }

        public (double percent, bool takenMoreThanTotal) PercentColonistsAvailable(IWorkTypeWrapper workType, Priority priorityIgnore)
        {
            var taken = 0d;
            var takenTotal = 0d;
            foreach (var it in WorkTables.Distinct(x => x.Priority)
                         .Where(x => x.WorkTypes[workType].variant != PercentVariant.PercentRemaining))
            {
                var percent = PercentValue(it.WorkTypes[workType], workType, priorityIgnore);
                if (it.Priority.v != priorityIgnore.v)
                    taken += percent;
                takenTotal += percent;
            }

            // available can't be negative
            return (Math.Max(1d - taken, 0d), takenTotal > 1.0001d);
        }

        public bool CanPawnBeAssigned(PawnFitnessData pawnFitnessData)
        {
            return (!pawnFitnessData.IsOpposed || IgnoreOppositionToWork)
                   && (pawnFitnessData.SkillLevel >= MinimumSkillLevel || !pawnFitnessData.IsSkilledWorkType);
        }

        public int NumberColonists(IWorkTypeWrapper workType)
        {
            return SortedPawnFitnessForEveryWork[workType].Count(CanPawnBeAssigned);
        }

        public bool PercentRemainExistsForWorkType(IWorkTypeWrapper workType)
        {
            return WorkTables.Any(workTableEntry => workTableEntry.WorkTypes[workType].variant == PercentVariant.PercentRemaining);
        }

        public double PercentValue(TablePercent tablePercent, IWorkTypeWrapper workTypeWrapper, Priority currentPriority)
        {
            var numberColonists = NumberColonists(workTypeWrapper);
            return tablePercent.variant switch
            {
                PercentVariant.Percent => tablePercent.PercentValue,
                PercentVariant.Number => numberColonists > 0 ? (double)tablePercent.NumberCount / numberColonists : 0,
                PercentVariant.PercentRemaining => PercentColonistsAvailable(workTypeWrapper, currentPriority).percent,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        private List<WorkTableEntry> LoadSavedState(IEnumerable<WorkTableEntry> loader)
        {
            Rebuild();
            List<WorkTableEntry>? workTables;
            try
            {
                workTables = loader.ToList();

                // if there are work types not present in built structure, then add with 0 percent
                foreach (var work in workTables.SelectMany(keyVal => WorkTypes.Where(work => !keyVal.WorkTypes.ContainsKey(work))))
                foreach (var it in workTables)
                {
                    _logger.Warn($"Work type {work} wasn't found in a save file. Setting percent to 0");
                    it.WorkTypes.Add(work, TablePercent.Percent(0));
                }
            }
            catch (Exception e)
            {
                _logger.Err("Error while loading percents state");
                _logger.Err(e);
                workTables = null;
            }

            return workTables ?? new List<WorkTableEntry>();
        }
    }
}
