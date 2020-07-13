using AutoPriorities.Core;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using AutoPriorities.Extensions;
using AutoPriorities.Percents;
using AutoPriorities.Utils;
using Verse;

namespace AutoPriorities
{
    public class PawnsData
    {
        public List<(int priority, Dictionary<WorkTypeDef, IPercent> workTypes)> WorkTables { get; }

        public HashSet<WorkTypeDef> WorkTypes { get; } = new HashSet<WorkTypeDef>();

        public HashSet<WorkTypeDef> WorkTypesNotRequiringSkills { get; } = new HashSet<WorkTypeDef>();

        public Dictionary<WorkTypeDef, List<(Pawn pawn, double fitness)>> SortedPawnFitnessForEveryWork { get; } =
            new Dictionary<WorkTypeDef, List<(Pawn, double)>>();

        public HashSet<Pawn> AllPlayerPawns { get; } = new HashSet<Pawn>();

        public PawnsData()
        {
            WorkTables = LoadSavedState() ?? new List<(int, Dictionary<WorkTypeDef, IPercent>)>();
        }

        private List<(int, Dictionary<WorkTypeDef, IPercent>)>? LoadSavedState()
        {
            Rebuild();
            List<(int priority, Dictionary<WorkTypeDef, IPercent> workTypes)>? workTables;
            try
            {
                workTables = PercentTableSaver.LoadState();

                // add totals, otherwise results in division by zero
                foreach (var (work, percent) in workTables.SelectMany(table => table.workTypes))
                    if (percent is Number n)
                        n.Initialize(new NumberPoolArgs {Count = n.Count, Total = NumberColonists(work)});

                // if not present in built structure, then add with 0 percent
                foreach (var work in workTables
                    .SelectMany(keyVal => WorkTypes
                        .Where(work => !keyVal.workTypes.ContainsKey(work))))
                foreach (var (_, d) in workTables)
                {
                    Controller.Log!.Message($"Work type {work} wasn't found in a save file. Setting percent to 0");
                    d.Add(work, Controller.PoolPercents.Acquire(new PercentPoolArgs {Value = 0}));
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                workTables = null;
            }
            catch (Exception e)
            {
                e.LogStackTrace();
                workTables = null;
            }

            return workTables;
        }

        public void SaveState()
        {
            try
            {
                PercentTableSaver.SaveState(WorkTables);
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
#endif

                // get all pawns owned by player
                var pawns = Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer);

                // get all skills associated with the work types
                AllPlayerPawns.Clear();
                SortedPawnFitnessForEveryWork.Clear();
                foreach (var work in workTypes)
                {
                    foreach (var pawn in pawns)
                    {
                        if (pawn.AnimalOrWildMan())
                            continue;

                        if (!AllPlayerPawns.Contains(pawn))
                            AllPlayerPawns.Add(pawn);

                        var fitness = -1d;
                        try
                        {
#if DEBUG
                            if (!pawn.IsCapableOfWholeWorkType(work))
                            {
                                // Controller.Log!.Message($"{pawn.NameFullColored} is incapable of {work}");
                            }
#endif
                            if (pawn.IsCapableOfWholeWorkType(work))
                            {
                                double skill = pawn.skills.AverageOfRelevantSkillsFor(work);
                                double passion = pawn.skills.MaxPassionOfRelevantSkillsFor(work) switch
                                {
                                    Passion.Minor => 1f,
                                    Passion.Major => 2f,
                                    _ => 0f
                                };

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
                            Controller.Log!.Message($"error: {e} for pawn {pawn.NameFullColored}");
                        }

                        if (SortedPawnFitnessForEveryWork.ContainsKey(work))
                        {
                            SortedPawnFitnessForEveryWork[work].Add((pawn, fitness));
                        }
                        else
                        {
                            SortedPawnFitnessForEveryWork.Add(work, new List<(Pawn, double)>
                            {
                                (pawn, fitness)
                            });
                        }
                    }

                    if (!WorkTypes.Contains(work))
                    {
                        WorkTypes.Add(work);
                        if (work.relevantSkills.Count == 0)
                            WorkTypesNotRequiringSkills.Add(work);
                    }
                }

                foreach (var keyValue in SortedPawnFitnessForEveryWork)
                {
                    keyValue.Value.Sort((x, y) => y.fitness.CompareTo(x.fitness));
                }
            }
            catch (Exception e)
            {
                Controller.Log!.Error("An error occured when rebuilding PawnData:");
                e.LogStackTrace();
            }
        }

        public double PercentColonistsAvailable(WorkTypeDef workType, int priorityIgnore)
        {
            var taken = 0d;
            foreach (var (priority, workTypes) in WorkTables)
            {
                if (priority == priorityIgnore)
                    continue;
                taken += workTypes[workType].Value;
                if (taken > 1f)
                    Log.Warning(
                        $"Percent of colonists assigned to work type {workType.defName} is greater than 1: {taken}");
            }

            return 1d - taken;
        }

        public int NumberColonists(WorkTypeDef workType) => SortedPawnFitnessForEveryWork[workType].Count;

        public int NumberOfColonistsAvailable(WorkTypeDef workType, int priorityIgnore)
        {
            return (int) (PercentColonistsAvailable(workType, priorityIgnore) *
                          NumberColonists(workType));
        }
    }
}