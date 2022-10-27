using System;
using System.Collections.Generic;
using System.Linq;
using AutoPriorities.APLogger;
using AutoPriorities.Core;
using AutoPriorities.Extensions;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.Percents;
using AutoPriorities.WorldInfoRetriever;
using AutoPriorities.Wrappers;
using RimWorld;

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

        public HashSet<IWorkTypeWrapper> WorkTypesNotRequiringSkills { get; } = new();

        public Dictionary<IWorkTypeWrapper, List<(IPawnWrapper pawn, double fitness)>> SortedPawnFitnessForEveryWork
        {
            get;
        } = new();

        public List<IPawnWrapper> CurrentMapPlayerPawns { get; } = new();
        public List<IPawnWrapper> AllPlayerPawns { get; } = new();

        public void SetData(SaveData data)
        {
            // Excluded must be loaded first because State depends on ExcludedPawns being filled
            ExcludedPawns = data.ExcludedPawns;
            WorkTables = LoadSavedState(data.WorkTablesData);

#if DEBUG
            _logger.Info(
                $"[SetData] first job count {WorkTables.FirstOrDefault().JobCount.v}; "
                + $"load job count: {data.WorkTablesData.FirstOrDefault().JobCount.v}");
#endif
        }

        public void SaveState()
        {
            try
            {
                _serializer.SaveData(
                    new SaveDataRequest { ExcludedPawns = ExcludedPawns, WorkTablesData = WorkTables });
            }
            catch (Exception e)
            {
                _logger.Err(e);
            }
        }

        public void Rebuild()
        {
            try
            {
                // get all work types
                var workTypes = _worldInfoRetriever.WorkTypeDefsInPriorityOrder()
                    .ToArray();

                var allPawns = _worldInfoRetriever.AllPawnsInPlayerFaction();
                AllPlayerPawns.Clear();
                AllPlayerPawns.AddRange(allPawns);

                // get all pawns owned by player
                var pawns = _worldInfoRetriever.PawnsInPlayerFactionInCurrentMap()
                    .ToList();

                // get all skills associated with the work types
                CurrentMapPlayerPawns.Clear();
                CurrentMapPlayerPawns.AddRange(pawns);

                SortedPawnFitnessForEveryWork.Clear();
                foreach (var work in workTypes)
                {
                    SortedPawnFitnessForEveryWork[work] = new List<(IPawnWrapper pawn, double fitness)>();
                    foreach (var pawn in pawns)
                    {
                        try
                        {
                            if (pawn.IsCapableOfWholeWorkType(work) && !ExcludedPawns.Contains(
                                    new ExcludedPawnEntry { WorkDef = work.DefName, PawnThingId = pawn.ThingID }))
                            {
                                var skill = pawn.AverageOfRelevantSkillsFor(work);
                                double passion = PassionFactor(pawn.MaxPassionOfRelevantSkillsFor(work));

                                var fitness = skill * passion;
                                SortedPawnFitnessForEveryWork[work]
                                    .Add((pawn, fitness));
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.Err($"error: {e} for pawn {pawn.NameFullColored}");
                            _logger.Err(e);
                        }
                    }

                    if (WorkTypes.Contains(work)) continue;

                    WorkTypes.Add(work);
                    if (work.RelevantSkillsCount == 0) WorkTypesNotRequiringSkills.Add(work);
                }

                foreach (var keyValue in SortedPawnFitnessForEveryWork)
                    keyValue.Value.Sort((x, y) => y.fitness.CompareTo(x.fitness));

                // _logger.Info(
                //     $"player pawns: {string.Join("; ", AllPlayerPawns.Select(x => $"{x.NameFullColored}:{x.ThingID}"))}");
                // remove all non player pawns
                ExcludedPawns.RemoveWhere(
                    wp =>
                    {
                        var isToBeDeleted = !AllPlayerPawns.Select(x => x.ThingID)
                            .Contains(wp.PawnThingId);
                        // if (res) _logger.Err($"INFO: removing {wp.pawnThingId} from excluded list");

                        return isToBeDeleted;
                    });
            }
            catch (Exception e)
            {
                _logger.Err("An error occured when rebuilding PawnData:");
                _logger.Err(e);
            }
        }

        public (double percent, bool takenMoreThanTotal) PercentColonistsAvailable(
            IWorkTypeWrapper workType,
            Priority priorityIgnore)
        {
            var taken = 0d;
            var takenTotal = 0d;
            foreach (var it in WorkTables
                         .Distinct(x => x.Priority)
                         .Where(x => x.WorkTypes[workType].Variant != PercentVariant.PercentRemaining))
            {
                var percent = PercentValue(it.WorkTypes[workType], workType, priorityIgnore);
                if (it.Priority.v != priorityIgnore.v) taken += percent;
                takenTotal += percent;
            }

            // available can't be negative
            return (Math.Max(1d - taken, 0d), takenTotal > 1.0001d);
        }

        public int NumberColonists(IWorkTypeWrapper workType)
        {
            return SortedPawnFitnessForEveryWork[workType]
                .Count;
        }

        public bool PercentRemainExistsForWorkType(IWorkTypeWrapper workType)
        {
            return WorkTables.Any(
                workTableEntry => workTableEntry.WorkTypes[workType].Variant == PercentVariant.PercentRemaining);
        }

        private static float PassionFactor(Passion passion)
        {
            return passion switch
            {
                Passion.Major => 1.5f,
                Passion.Minor => 1f,
                Passion.None => 0.35f,
                _ => 0f
            };
        }

        public double PercentValue(TablePercent tablePercent,
            IWorkTypeWrapper workTypeWrapper,
            Priority currentPriority)
        {
            return tablePercent.Variant switch
            {
                PercentVariant.Percent => tablePercent.PercentValue,
                PercentVariant.Number => (double)tablePercent.NumberCount / NumberColonists(workTypeWrapper),
                PercentVariant.PercentRemaining => PercentColonistsAvailable(workTypeWrapper, currentPriority).percent,
                _ => throw new ArgumentOutOfRangeException()
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
                foreach (var work in workTables.SelectMany(
                             keyVal => WorkTypes.Where(work => !keyVal.WorkTypes.ContainsKey(work))))
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
