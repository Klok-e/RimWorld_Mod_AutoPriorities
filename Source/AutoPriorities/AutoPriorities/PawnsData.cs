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

        public List<(Priority priority, JobCount maxJobs, Dictionary<IWorkTypeWrapper, TablePercent> workTypes)>
            WorkTables { get; private set; } = new();

        public HashSet<(IWorkTypeWrapper work, IPawnWrapper pawn)> ExcludedPawns { get; private set; } = new();

        public HashSet<IWorkTypeWrapper> WorkTypes { get; } = new();

        public HashSet<IWorkTypeWrapper> WorkTypesNotRequiringSkills { get; } = new();

        public Dictionary<IWorkTypeWrapper, List<(IPawnWrapper pawn, double fitness)>> SortedPawnFitnessForEveryWork
        {
            get;
        } =
            new();

        public HashSet<IPawnWrapper> AllPlayerPawns { get; } = new();

        public void SetData(SaveData data)
        {
            // Excluded must be loaded first because State depends on ExcludedPawns being filled
            ExcludedPawns = data.ExcludedPawns;
            WorkTables = LoadSavedState(data.WorkTablesData);
        }

        public void SaveState()
        {
            try
            {
                _serializer.SaveData(new SaveDataRequest
                {
                    ExcludedPawns = ExcludedPawns,
                    WorkTablesData = WorkTables
                });
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

                // get all pawns owned by player
                var pawns = _worldInfoRetriever.PawnsInPlayerFaction()
                                               .ToArray();

                // get all skills associated with the work types
                AllPlayerPawns.Clear();
                SortedPawnFitnessForEveryWork.Clear();
                foreach (var work in workTypes)
                {
                    SortedPawnFitnessForEveryWork[work] = new List<(IPawnWrapper pawn, double fitness)>();
                    foreach (var pawn in pawns.Where(pawn => !pawn.AnimalOrWildMan()))
                    {
                        if (!AllPlayerPawns.Contains(pawn)) AllPlayerPawns.Add(pawn);

                        var fitness = -1d;
                        try
                        {
                            if (pawn.IsCapableOfWholeWorkType(work) && !ExcludedPawns.Contains((work, pawn)))
                            {
                                var skill = pawn.AverageOfRelevantSkillsFor(work);
                                double passion = PassionFactor(pawn.MaxPassionOfRelevantSkillsFor(work));

                                fitness = skill * (1 + passion * Math.Max(_worldInfoRetriever.PassionMultiplier, 0d));
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.Err($"error: {e} for pawn {pawn.NameFullColored}");
                            _logger.Err(e);
                        }

                        if (fitness >= 0d)
                            SortedPawnFitnessForEveryWork[work]
                                .Add((pawn, fitness));
                    }

                    if (WorkTypes.Contains(work)) continue;

                    WorkTypes.Add(work);
                    if (work.relevantSkillsCount == 0) WorkTypesNotRequiringSkills.Add(work);
                }

                foreach (var keyValue in SortedPawnFitnessForEveryWork)
                    keyValue.Value.Sort((x, y) => y.fitness.CompareTo(x.fitness));

                // remove all non player pawns
                ExcludedPawns.RemoveWhere(wp => !AllPlayerPawns.Contains(wp.pawn));
            }
            catch (Exception e)
            {
                _logger.Err("An error occured when rebuilding PawnData:");
                _logger.Err(e);
            }
        }

        public (double percent, bool takenMoreThanTotal) PercentColonistsAvailable(IWorkTypeWrapper workType,
            Priority priorityIgnore)
        {
            var taken = 0d;
            var takenTotal = 0d;
            foreach (var (priority, _, workTypes) in WorkTables
                .Distinct(x => x.priority))
            {
                var percent = workTypes[workType]
                    .Value;
                if (priority.V != priorityIgnore.V) taken += percent;
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

        public static float PassionFactor(Passion passion)
        {
            return passion switch
            {
                Passion.Minor => 1f,
                Passion.Major => 2f,
                _ => 0f
            };
        }

        private List<(Priority priority, JobCount maxJobs, Dictionary<IWorkTypeWrapper, TablePercent> workTypes)>
            LoadSavedState(
                IEnumerable<(Priority priority, JobCount maxJobs, Dictionary<IWorkTypeWrapper, TablePercent> workTypes)>
                    loader)
        {
            Rebuild();
            List<(Priority priority, JobCount maxJobs, Dictionary<IWorkTypeWrapper, TablePercent> workTypes)>?
                workTables;
            try
            {
                workTables = loader.ToList();

                // add totals, otherwise results in division by zero
                for (var i = 0; i < workTables.Count; i++)
                {
                    var currentWorkTable = workTables[i]
                        .workTypes;
                    
                    // ToArray is needed to not modify the collection while iterating
                    foreach (var key in currentWorkTable
                                        .Keys.ToArray())
                    {
                        var currentPercent = currentWorkTable[key];
                        if (currentPercent.Variant == PercentVariant.Number)
                        {
                            currentWorkTable[key] =
                                TablePercent.Number(NumberColonists(key), currentPercent.NumberCount);
                        }
                    }
                }

                // if there are work types not present in built structure, then add with 0 percent
                foreach (var work in workTables
                    .SelectMany(keyVal => WorkTypes
                        .Where(work => !keyVal.workTypes.ContainsKey(work))))
                foreach (var (_, _, d) in workTables)
                {
                    _logger.Warn($"Work type {work} wasn't found in a save file. Setting percent to 0");
                    d.Add(work, TablePercent.Percent(0));
                }
            }
            catch (Exception e)
            {
                _logger.Err("Error while loading percents state");
                _logger.Err(e);
                workTables = null;
            }

            return workTables ??
                   new List<(Priority priority, JobCount maxJobs, Dictionary<IWorkTypeWrapper, TablePercent> workTypes
                       )>();
        }
    }
}
