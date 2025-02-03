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
        private readonly IWorldInfoRetriever _worldInfoRetriever;

        public PawnsData(IPawnsDataSerializer serializer, IWorldInfoRetriever worldInfoRetriever, ILogger logger)
        {
            _serializer = serializer;
            _worldInfoRetriever = worldInfoRetriever;
            _logger = logger;
        }

        public List<WorkTableEntry> WorkTables { get; private set; } = new();

        public HashSet<ExcludedPawnEntry> ExcludedPawns { get; private set; } = new();

        public HashSet<IWorkTypeWrapper> WorkTypes { get; } = new();

        public HashSet<IWorkTypeWrapper> WorkTypesNotRequiringSkills { get; private set; } = new();

        public Dictionary<IWorkTypeWrapper, List<PawnFitnessData>> SortedPawnFitnessForEveryWork { get; private set; } = new();

        public List<IPawnWrapper> CurrentMapPlayerPawns { get; private set; } = new();

        public List<IPawnWrapper> AllPlayerPawns { get; private set; } = new();

        public bool IgnoreLearningRate { get; set; }

        public bool IgnoreOppositionToWork { get; set; }

        public float MinimumSkillLevel { get; set; }

        public PawnsData ShallowCopy()
        {
            var shallowCopy = new PawnsData(_serializer, _worldInfoRetriever, _logger)
            {
                WorkTables = WorkTables.Select(x => x.ShallowCopy()).ToList(),
                ExcludedPawns = ExcludedPawns.ToHashSet(),
                WorkTypesNotRequiringSkills = WorkTypesNotRequiringSkills.ToHashSet(),
                SortedPawnFitnessForEveryWork = SortedPawnFitnessForEveryWork.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                CurrentMapPlayerPawns = CurrentMapPlayerPawns.ToList(),
                AllPlayerPawns = AllPlayerPawns.ToList(),
                IgnoreLearningRate = IgnoreLearningRate,
                IgnoreOppositionToWork = IgnoreOppositionToWork,
                MinimumSkillLevel = MinimumSkillLevel,
            };

            return shallowCopy;
        }

        public void SetData(SaveData data)
        {
            // Excluded must be loaded first because State depends on ExcludedPawns being filled
            ExcludedPawns = data.ExcludedPawns;
            WorkTables = LoadSavedState(data.WorkTablesData);
            IgnoreLearningRate = data.IgnoreLearningRate;
            MinimumSkillLevel = data.MinimumSkillLevel;
            IgnoreOppositionToWork = data.IgnoreOppositionToWork;

#if DEBUG
            _logger.Info(
                $"[SetData] first job count {WorkTables.FirstOrDefault().JobCount.v}; "
                + $"load job count: {data.WorkTablesData.FirstOrDefault().JobCount.v}"
            );
#endif
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
            };
        }

        public void Rebuild()
        {
            try
            {
                // get all work types
                var workTypes = _worldInfoRetriever.GetWorkTypeDefsInPriorityOrder().ToArray();

                var allPawns = _worldInfoRetriever.GetAllAdultPawnsInPlayerFaction();
                AllPlayerPawns.Clear();
                AllPlayerPawns.AddRange(allPawns);

                // get all pawns owned by player
                var pawns = _worldInfoRetriever.GetAdultPawnsInPlayerFactionInCurrentMap();

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
                                || ExcludedPawns.Contains(new ExcludedPawnEntry { WorkDef = work.DefName, PawnThingId = pawn.ThingID }))
                            {
                                continue;
                            }

                            var skill = pawn.AverageOfRelevantSkillsFor(work);
                            var learningRateFactor = IgnoreLearningRate ? 1 : pawn.MaxLearningRateFactor(work);

                            var isSkilledWorkType = work.RelevantSkillsCount > 0;
                            var fitness = isSkilledWorkType ? skill * learningRateFactor : 0.001f;

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

                // _logger.Info(
                //     $"player pawns: {string.Join("; ", AllPlayerPawns.Select(x => $"{x.NameFullColored}:{x.ThingID}"))}");
                // remove all non player pawns
                ExcludedPawns.RemoveWhere(
                    wp =>
                    {
                        var isToBeDeleted = !AllPlayerPawns.Select(x => x.ThingID).Contains(wp.PawnThingId);
                        // if (isToBeDeleted)
                        //     _logger.Info($"removing {wp.PawnThingId} from excluded list");

                        return isToBeDeleted;
                    }
                );
            }
            catch (Exception e)
            {
                _logger.Err("An error occured when rebuilding PawnData:");
                _logger.Err(e);
            }
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

        public int NumberColonists(IWorkTypeWrapper workType)
        {
            return SortedPawnFitnessForEveryWork[workType].Count;
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
