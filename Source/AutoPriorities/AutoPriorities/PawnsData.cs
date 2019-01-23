using AutoPriorities.Core;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace AutoPriorities
{
    internal class PawnsData
    {
        public List<Tuple2<int, Dictionary<WorkTypeDef, float>>> PriorityToWorkTypesAndPercentOfPawns { get; private set; }

        public HashSet<WorkTypeDef> WorkTypes { get; private set; }

        public HashSet<WorkTypeDef> WorkTypesNotRequiringSkills { get; private set; }

        public Dictionary<WorkTypeDef, List<Tuple2<Pawn, float>>> SortedPawnFitnessForEveryWork { get; private set; }

        public HashSet<Pawn> AllPlayerPawns { get; private set; }

        public PawnsData()
        {
            AllPlayerPawns = new HashSet<Pawn>();
            WorkTypes = new HashSet<WorkTypeDef>();
            WorkTypesNotRequiringSkills = new HashSet<WorkTypeDef>();
            SortedPawnFitnessForEveryWork = new Dictionary<WorkTypeDef, List<Tuple2<Pawn, float>>>();

            PriorityToWorkTypesAndPercentOfPawns = new List<Tuple2<int, Dictionary<WorkTypeDef, float>>>();

            LoadSavedState();
        }

        private void LoadSavedState()
        {
            Rebuild();
            try
            {
                PriorityToWorkTypesAndPercentOfPawns = PercentPerWorkTypeSaver.LoadState();

                //check whether state is correct
                bool correct = true;
                foreach(var keyVal in PriorityToWorkTypesAndPercentOfPawns)
                {
                    foreach(var work in WorkTypes)
                    {
                        if(!keyVal._val2.ContainsKey(work))
                        {
                            Log.Message($"AutoPriorities: {work.labelShort} has been found but was not present in a save file");
                            correct = false;
                            goto outOfCycles;
                        }
                    }
                }
                outOfCycles:
                if(!correct)
                {
                    PriorityToWorkTypesAndPercentOfPawns = new List<Tuple2<int, Dictionary<WorkTypeDef, float>>>();
                    Log.Message("AutoPriorities: Priorities have been reset.");
                }
            }
            catch(System.IO.FileNotFoundException)
            {
                PriorityToWorkTypesAndPercentOfPawns = new List<Tuple2<int, Dictionary<WorkTypeDef, float>>>();
            }
            catch(Exception e)
            {
                Log.Error(e.Message);
                PriorityToWorkTypesAndPercentOfPawns = new List<Tuple2<int, Dictionary<WorkTypeDef, float>>>();
            }
        }

        public void SaveState()
        {
            try
            {
                PercentPerWorkTypeSaver.SaveState(PriorityToWorkTypesAndPercentOfPawns);
            }
            catch(Exception e)
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
            foreach(var work in workTypes)
            {
                foreach(var pawn in pawns)
                {
                    if(pawn.AnimalOrWildMan())
                        continue;

                    if(!AllPlayerPawns.Contains(pawn))
                        AllPlayerPawns.Add(pawn);

                    float fitness = 0;
                    try
                    {
                        float skill = pawn.skills.AverageOfRelevantSkillsFor(work);
                        float passion = 0f;
                        switch(pawn.skills.MaxPassionOfRelevantSkillsFor(work))
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
                    catch(Exception e)
                    {
                        Log.Message($"error: {e} for pawn {pawn.Name.ToStringFull}");
                    }
                    if(SortedPawnFitnessForEveryWork.ContainsKey(work))
                    {
                        SortedPawnFitnessForEveryWork[work].Add(new Tuple2<Pawn, float>(pawn, fitness));
                    }
                    else
                    {
                        SortedPawnFitnessForEveryWork.Add(work, new List<Tuple2<Pawn, float>>
                        {
                            new Tuple2<Pawn, float>(pawn, fitness),
                        });
                    }

                }
                if(!WorkTypes.Contains(work))
                {
                    WorkTypes.Add(work);
                    if(work.relevantSkills.Count == 0)
                        WorkTypesNotRequiringSkills.Add(work);
                }
            }

            foreach(var keyValue in SortedPawnFitnessForEveryWork)
            {
                keyValue.Value.Sort((x, y) => y._val2.CompareTo(x._val2));
            }
        }

        public float PercentOfColonistsAvailable(WorkTypeDef workType, int priorityIgnore)
        {
            float taken = 0;
            foreach(var tuple in PriorityToWorkTypesAndPercentOfPawns)
            {
                if(tuple._val1 == priorityIgnore)
                    continue;
                taken += tuple._val2[workType];
                if(taken > 1f)
                    Log.Error($"Percent of colonists assigned to work type {workType.defName} is greater than 1: {taken}");
            }
            return 1f - taken;
        }
    }
}
