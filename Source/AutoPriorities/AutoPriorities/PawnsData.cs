using System;
using System.Collections.Generic;
using System.Linq;
using AutoPriorities.Core;
using AutoPriorities.Extensions;
using AutoPriorities.Percents;
using AutoPriorities.Utils;
using RimWorld;
using Verse;

namespace AutoPriorities
{
    public class PawnsData
    {
        public PawnsData()
        {
            var (percents, excluded) = PercentTableSaver.GetStateLoaders();
            // Excluded must be loaded first because State depends on ExcludedPawns being filled
            ExcludedPawns = LoadSavedExcluded(excluded);
            WorkTables = LoadSavedState(percents);
        }

        public List<(Priority priority, JobCount maxJobs, Dictionary<WorkTypeDef, IPercent> workTypes)> WorkTables
        {
            get;
        }

        public HashSet<(WorkTypeDef work, Pawn pawn)> ExcludedPawns { get; }

        public HashSet<WorkTypeDef> WorkTypes { get; } = new HashSet<WorkTypeDef>();

        public HashSet<WorkTypeDef> WorkTypesNotRequiringSkills { get; } = new HashSet<WorkTypeDef>();

        public Dictionary<WorkTypeDef, List<(Pawn pawn, double fitness)>> SortedPawnFitnessForEveryWork { get; } =
            new Dictionary<WorkTypeDef, List<(Pawn, double)>>();

        public HashSet<Pawn> AllPlayerPawns { get; } = new HashSet<Pawn>();

        public void SaveState()
        {
            try
            {
                PercentTableSaver.SaveState((WorkTables, ExcludedPawns));
            }
            catch (Exception e)
            {
                e.LogStackTrace();
            }
        }

        public void Rebuild()
        {
            try
            {
                // get all work types
                var workTypes = WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder.ToArray();

#if DEBUG
                Controller.Log!.Message(
                    $"work types: {string.Join(" ", workTypes.Select(w => w.defName))}");
                var exclStr = ExcludedPawns.Select(wp => $"({wp.Item1.defName}; {wp.Item2.LabelNoCount})");
                Controller.Log!.Message(
                    $"excluded pawns: {string.Join(" ", exclStr)}");
#endif

                // get all pawns owned by player
                var pawns = Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer);

                // get all skills associated with the work types
                AllPlayerPawns.Clear();
                SortedPawnFitnessForEveryWork.Clear();
                foreach (var work in workTypes)
                {
                    SortedPawnFitnessForEveryWork[work] = new List<(Pawn pawn, double fitness)>();
                    foreach (var pawn in pawns.Where(pawn => !pawn.AnimalOrWildMan()))
                    {
                        if (!AllPlayerPawns.Contains(pawn)) AllPlayerPawns.Add(pawn);

                        var fitness = -1d;
                        try
                        {
#if DEBUG
                            if (!pawn.IsCapableOfWholeWorkType(work))
                            {
                                // Controller.Log!.Message($"{pawn.NameFullColored} is incapable of {work}");
                            }
#endif
                            if (pawn.IsCapableOfWholeWorkType(work) && !ExcludedPawns.Contains((work, pawn)))
                            {
                                double skill = pawn.skills.AverageOfRelevantSkillsFor(work);
                                double passion = PassionFactor(pawn.skills.MaxPassionOfRelevantSkillsFor(work));

                                fitness = skill + skill * passion * Math.Max(Controller.PassionMult, 0d);

#if DEBUG
                                if (work.defName == "Firefighter")
                                {
                                    // Controller.Log!.Message(
                                    //     $"{pawn.NameFullColored} is capable of {work} with fitness of {fitness}" +
                                    //     $" (skill avg {skill}, passion {passion}, mult {Controller.PassionMult})");
                                }
#endif
                            }
                        }
                        catch (Exception e)
                        {
                            Controller.Log!.Error($"error: {e} for pawn {pawn.NameFullColored}");
                            e.LogStackTrace();
                        }

                        if (fitness >= 0d)
                            SortedPawnFitnessForEveryWork[work]
                                .Add((pawn, fitness));
                    }

                    if (!WorkTypes.Contains(work))
                    {
                        WorkTypes.Add(work);
                        if (work.relevantSkills.Count == 0) WorkTypesNotRequiringSkills.Add(work);
                    }
                }

                foreach (var keyValue in SortedPawnFitnessForEveryWork)
                    keyValue.Value.Sort((x, y) => y.fitness.CompareTo(x.fitness));

                // remove all non player pawns
                ExcludedPawns.RemoveWhere(wp => !AllPlayerPawns.Contains(wp.pawn));
            }
            catch (Exception e)
            {
                Controller.Log!.Error("An error occured when rebuilding PawnData:");
                e.LogStackTrace();
            }
        }

        public (double percent, bool takenMoreThanTotal) PercentColonistsAvailable(WorkTypeDef workType,
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

        public int NumberColonists(WorkTypeDef workType)
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

        private HashSet<(WorkTypeDef, Pawn)> LoadSavedExcluded(Func<HashSet<(WorkTypeDef, Pawn)>> loader)
        {
            HashSet<(WorkTypeDef, Pawn)>? excluded;
            try
            {
                excluded = loader();
            }
            catch (Exception e)
            {
                Controller.Log!.Error("Error while loading percents state");
                e.LogStackTrace();
                excluded = null;
            }

            return excluded ?? new HashSet<(WorkTypeDef, Pawn)>();
        }

        private List<(Priority priority, JobCount maxJobs, Dictionary<WorkTypeDef, IPercent> workTypes)> LoadSavedState(
            Func<List<(Priority priority, JobCount? maxJobs, Dictionary<WorkTypeDef, IPercent> workTypes)>> loader)
        {
            Rebuild();
            List<(Priority priority, JobCount maxJobs, Dictionary<WorkTypeDef, IPercent> workTypes)>? workTables;
            try
            {
                workTables = loader()
                             // fill max jobs with default value if there's no value already
                             .Select(t => (t.priority, t.maxJobs ?? WorkTypes.Count, t.workTypes))
                             .ToList();

                // add totals, otherwise results in division by zero
                foreach (var (work, percent) in workTables.SelectMany(table => table.workTypes))
                    if (percent is Number n)
                        n.Initialize(new NumberPoolArgs {Count = n.Count, Total = NumberColonists(work)});

                // if not present in built structure, then add with 0 percent
                foreach (var work in workTables
                    .SelectMany(keyVal => WorkTypes
                        .Where(work => !keyVal.workTypes.ContainsKey(work))))
                foreach (var (_, _, d) in workTables)
                {
                    Controller.Log!.Message($"Work type {work} wasn't found in a save file. Setting percent to 0");
                    d.Add(work, Controller.PoolPercents.Acquire(new PercentPoolArgs {Value = 0}));
                }
            }
            catch (Exception e)
            {
                Controller.Log!.Error("Error while loading percents state");
                e.LogStackTrace();
                workTables = null;
            }

            return workTables ??
                   new List<(Priority priority, JobCount maxJobs, Dictionary<WorkTypeDef, IPercent> workTypes)>();
        }
    }
}
