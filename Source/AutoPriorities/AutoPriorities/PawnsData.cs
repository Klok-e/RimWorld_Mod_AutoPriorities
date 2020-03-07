using AutoPriorities.Core;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace AutoPriorities
{
    internal class PawnsData
    {
        public List<(int priority, Dictionary<WorkTypeDef, float> workTypes)> WorkTables { get; private set; }

        public HashSet<WorkTypeDef> WorkTypes { get; private set; }

        public HashSet<WorkTypeDef> WorkTypesNotRequiringSkills { get; private set; }

        public Dictionary<WorkTypeDef, List<(Pawn pawn, float fitness)>> SortedPawnFitnessForEveryWork
        {
            get;
            private set;
        }

        public HashSet<Pawn> AllPlayerPawns { get; private set; }

        public PawnsData()
        {
            AllPlayerPawns = new HashSet<Pawn>();
            WorkTypes = new HashSet<WorkTypeDef>();
            WorkTypesNotRequiringSkills = new HashSet<WorkTypeDef>();
            SortedPawnFitnessForEveryWork = new Dictionary<WorkTypeDef, List<(Pawn, float)>>();

            WorkTables = new List<(int, Dictionary<WorkTypeDef, float>)>();

            LoadSavedState();
        }

        private void LoadSavedState()
        {
            Rebuild();
            try
            {
                WorkTables = PercentPerWorkTypeSaver.LoadState();

                //check whether state is correct
                bool correct = true;
                foreach (var keyVal in WorkTables)
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
                    WorkTables = new List<(int, Dictionary<WorkTypeDef, float>)>();
                    Log.Message("AutoPriorities: Priorities have been reset.");
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                WorkTables = new List<(int, Dictionary<WorkTypeDef, float>)>();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                WorkTables = new List<(int, Dictionary<WorkTypeDef, float>)>();
            }
        }

        public void SaveState()
        {
            try
            {
                PercentPerWorkTypeSaver.SaveState(WorkTables);
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

                        fitness = skill + skill * passion * Controller.Settings._passionMult;
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
                taken += tuple.workTypes[workType];
                if (taken > 1f)
                    Log.Error(
                        $"Percent of colonists assigned to work type {workType.defName} is greater than 1: {taken}");
            }

            return 1f - taken;
        }
    }
}