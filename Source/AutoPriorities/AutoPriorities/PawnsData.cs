using AutoPriorities.Core;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AutoPriorities.Extensions;
using AutoPriorities.Percents;
using AutoPriorities.Utils;
using Verse;

namespace AutoPriorities
{
    public class PawnsData
    {
        public List<(int priority, Dictionary<WorkTypeDef, IPercent> workTypes)> WorkTables { get; }

        public HashSet<WorkTypeDef> WorkTypes { get; }

        public HashSet<WorkTypeDef> WorkTypesNotRequiringSkills { get; }

        public Dictionary<WorkTypeDef, List<(Pawn pawn, double fitness)>> SortedPawnFitnessForEveryWork { get; }

        public HashSet<Pawn> AllPlayerPawns { get; }

        public PawnsData()
        {
            AllPlayerPawns = new HashSet<Pawn>();
            WorkTypes = new HashSet<WorkTypeDef>();
            WorkTypesNotRequiringSkills = new HashSet<WorkTypeDef>();
            SortedPawnFitnessForEveryWork = new Dictionary<WorkTypeDef, List<(Pawn, double)>>();

            WorkTables = LoadSavedState() ?? new List<(int, Dictionary<WorkTypeDef, IPercent>)>();
        }

        private List<(int, Dictionary<WorkTypeDef, IPercent>)>? LoadSavedState()
        {
            Rebuild();
            List<(int priority, Dictionary<WorkTypeDef, IPercent> workTypes)>? workTables;
            try
            {
                workTables = PercentTableSaver.LoadState();

                // if not present in built structure, then add with 0 percent
                foreach (var work in workTables
                    .SelectMany(keyVal => WorkTypes
                        .Where(work => !keyVal.workTypes.ContainsKey(work))))
                foreach (var (_, d) in workTables)
                {
                    Controller.Log.Message($"Work type {work} wasn't found in a save file. Setting percent to 0");
                    d.Add(work, new Percent(0));
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
                var workTypes = WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder;

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

                        double fitness = 0;
                        try
                        {
                            if (pawn.IsCapableOfWholeWorkType(work))
                            {
                                double skill = pawn.skills.AverageOfRelevantSkillsFor(work);
                                double passion = 0f;
                                switch (pawn.skills.MaxPassionOfRelevantSkillsFor(work))
                                {
                                    case Passion.Minor:
                                        passion = 1f;
                                        break;
                                    case Passion.Major:
                                        passion = 2f;
                                        break;
                                }

                                fitness = skill + skill * passion * Math.Max(Controller.PassionMult, 0d);
                            }
                            else
                            {
                                fitness = 0;
                            }
                        }
                        catch (Exception e)
                        {
                            Controller.Log.Message($"error: {e} for pawn {pawn.Name.ToStringFull}");
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
                Controller.Log.Error($"An error occured when rebuilding PawnData:");
                e.LogStackTrace();
            }
        }

        public double PercentOfColonistsAvailable(WorkTypeDef workType, int priorityIgnore)
        {
            var taken = 0d;
            foreach (var tuple in WorkTables)
            {
                if (tuple.priority == priorityIgnore)
                    continue;
                taken += tuple.workTypes[workType].Value;
                if (taken > 1f)
                    Log.Warning(
                        $"Percent of colonists assigned to work type {workType.defName} is greater than 1: {taken}");
            }

            return 1d - taken;
        }
    }
}