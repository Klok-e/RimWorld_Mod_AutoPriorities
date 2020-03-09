using AutoPriorities.Core;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using AutoPriorities.Percents;
using Verse;

namespace AutoPriorities
{
    public class PawnsData
    {
        public List<(int priority, Dictionary<WorkTypeDef, IPercent> workTypes)> WorkTables { get; }

        public HashSet<WorkTypeDef> WorkTypes { get; }

        public HashSet<WorkTypeDef> WorkTypesNotRequiringSkills { get; }

        public Dictionary<WorkTypeDef, List<(Pawn pawn, float fitness)>> SortedPawnFitnessForEveryWork { get; }

        public HashSet<Pawn> AllPlayerPawns { get; }

        public PawnsData()
        {
            AllPlayerPawns = new HashSet<Pawn>();
            WorkTypes = new HashSet<WorkTypeDef>();
            WorkTypesNotRequiringSkills = new HashSet<WorkTypeDef>();
            SortedPawnFitnessForEveryWork = new Dictionary<WorkTypeDef, List<(Pawn, float)>>();

            WorkTables = LoadSavedState() ?? new List<(int, Dictionary<WorkTypeDef, IPercent>)>();
        }

        private List<(int, Dictionary<WorkTypeDef, IPercent>)>? LoadSavedState()
        {
            Rebuild();
            List<(int priority, Dictionary<WorkTypeDef, IPercent> workTypes)>? workTables = null;
            try
            {
                // TODO: make loader load IPercents instead of converting
                workTables = PercentPerWorkTypeSaver
                    .LoadState()
                    .Select(a => (a.Item1, a.Item2
                        .Select(b => (b.Key, (IPercent) new Percent(b.Value)))
                        .ToDictionary(x => x.Key, y => y.Item2))).ToList();

                //check whether state is correct
                bool correct = true;
                foreach (var keyVal in workTables)
                {
                    foreach (var work in WorkTypes)
                    {
                        if (!keyVal.workTypes.ContainsKey(work))
                        {
                            Log.Message(
                                $"AutoPriorities: {work.labelShort} has been found but was not present in a save file");
                            correct = false;
                            goto outOfCycles;
                        }
                    }
                }

                outOfCycles:
                if (!correct)
                {
                    Log.Message("AutoPriorities: Priorities have been reset.");
                }
            }
            catch (System.IO.FileNotFoundException)
            {
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            return workTables;
        }

        public void SaveState()
        {
            try
            {
                // TODO: make loader load IPercents instead of converting
                PercentPerWorkTypeSaver.SaveState(WorkTables
                    .Select(a => (a.priority, a.workTypes
                        .Select(b => (b.Key, b.Value.Value))
                        .ToDictionary(x => x.Key, y => y.Value)))
                    .ToList());
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }

        public void Rebuild()
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

                    float fitness = 0;
                    try
                    {
                        float skill = pawn.skills.AverageOfRelevantSkillsFor(work);
                        float passion = 0f;
                        switch (pawn.skills.MaxPassionOfRelevantSkillsFor(work))
                        {
                            case Passion.Minor:
                                passion = 1f;
                                break;
                            case Passion.Major:
                                passion = 2f;
                                break;
                        }

                        fitness = skill + skill * passion * Controller.PassionMult;
                    }
                    catch (Exception e)
                    {
                        Log.Message($"error: {e} for pawn {pawn.Name.ToStringFull}");
                    }

                    if (SortedPawnFitnessForEveryWork.ContainsKey(work))
                    {
                        SortedPawnFitnessForEveryWork[work].Add((pawn, fitness));
                    }
                    else
                    {
                        SortedPawnFitnessForEveryWork.Add(work, new List<(Pawn, float)>
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

        public float PercentOfColonistsAvailable(WorkTypeDef workType, int priorityIgnore)
        {
            float taken = 0;
            foreach (var tuple in WorkTables)
            {
                if (tuple.priority == priorityIgnore)
                    continue;
                taken += tuple.workTypes[workType].Value;
                if (taken > 1f)
                    Log.Error(
                        $"Percent of colonists assigned to work type {workType.defName} is greater than 1: {taken}");
            }

            return 1f - taken;
        }
    }
}