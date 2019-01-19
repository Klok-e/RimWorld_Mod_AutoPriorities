using AutoPriorities.Extensions;
using AutoPriorities.Utils;
using System;
using System.Collections.Generic;
using Verse;

namespace AutoPriorities
{
    internal class PrioritiesAssigner
    {
        private HashSet<int> PrioritiesEncounteredCached { get; }

        private PawnsData PawnsData { get; }

        public PrioritiesAssigner(PawnsData pawnsData)
        {
            PrioritiesEncounteredCached = new HashSet<int>();
            PawnsData = pawnsData;
        }

        public void AssignPriorities()
        {
            try
            {
                var listOfPawnAndAmountOfJobsAssigned = new Dictionary<Pawn, int>(PawnsData.AllPlayerPawns.Count);
                foreach(var item in PawnsData.AllPlayerPawns)
                    listOfPawnAndAmountOfJobsAssigned.Add(item, 0);

                var priorityToPercentOfColonists = new List<Tuple2<int, float>>();
                foreach(var work in PawnsData.WorkTypes)
                {
                    //skip works not requiring skills because they will be handled later
                    if(PawnsData.WorkTypesNotRequiringSkills.Contains(work))
                        continue;

                    FillListOfPriorityToPercentOfColonists(work, priorityToPercentOfColonists);

                    var pawns = PawnsData.SortedPawnSkillForEveryWork[work];
                    float pawnsCount = pawns.Count;

                    PrioritiesEncounteredCached.Clear();
                    int pawnsIterated = 0;
                    float mustBeIteratedForThisPriority = 0f;
                    foreach(var priorityToPercent in priorityToPercentOfColonists)
                    {
                        //skip repeating priorities
                        if(PrioritiesEncounteredCached.Contains(priorityToPercent._val1))
                            continue;

                        mustBeIteratedForThisPriority += priorityToPercent._val2 * pawnsCount;
                        //Log.Message($"mustBeIteratedForThisPriority {priorityToPercent._val1}: {mustBeIteratedForThisPriority}; pawnsCount: {pawnsCount}");
                        for(; pawnsIterated < mustBeIteratedForThisPriority; pawnsIterated++)
                        {
                            var currentPawn = pawns[pawnsIterated];

                            //skip incapable pawns
                            if(currentPawn._val1.IsCapableOfWholeWorkType(work))
                            {
                                //Log.Message($"in loop mustBeIteratedForThisPriority {priorityToPercent._val1}: {mustBeIteratedForThisPriority}; pawnsIterated: {pawnsIterated}");
                                currentPawn._val1.workSettings.SetPriority(work, priorityToPercent._val1);

                                listOfPawnAndAmountOfJobsAssigned[currentPawn._val1] += 1;
                            }
                        }
                        PrioritiesEncounteredCached.Add(priorityToPercent._val1);
                    }
                    //set remaining pawns to 0
                    if(pawnsIterated < pawnsCount)
                    {
                        for(; pawnsIterated < pawnsCount; pawnsIterated++)
                        {
                            if(!pawns[pawnsIterated]._val1.IsCapableOfWholeWorkType(work))
                                continue;
                            pawns[pawnsIterated]._val1.workSettings.SetPriority(work, 0);
                        }
                    }
                }

                //turn dict to list
                var jobsCount = new List<Tuple2<Pawn, int>>(listOfPawnAndAmountOfJobsAssigned.Count);
                foreach(var item in listOfPawnAndAmountOfJobsAssigned)
                    jobsCount.Add(new Tuple2<Pawn, int>(item.Key, item.Value));

                //sort by ascending to then iterate (lower count of works assigned gets works first)
                jobsCount.Sort((x, y) => x._val2.CompareTo(y._val2));
                foreach(var work in PawnsData.WorkTypesNotRequiringSkills)
                {
                    FillListOfPriorityToPercentOfColonists(work, priorityToPercentOfColonists);

                    PrioritiesEncounteredCached.Clear();
                    int pawnsIterated = 0;
                    float mustBeIteratedForThisPriority = 0f;
                    foreach(var priorityToPercent in priorityToPercentOfColonists)
                    {
                        //skip repeating priorities
                        if(PrioritiesEncounteredCached.Contains(priorityToPercent._val1))
                            continue;

                        mustBeIteratedForThisPriority += priorityToPercent._val2 * jobsCount.Count;
                        for(; pawnsIterated < mustBeIteratedForThisPriority; pawnsIterated++)
                        {
                            var currentPawn = jobsCount[pawnsIterated];

                            //skip incapable pawns
                            if(currentPawn._val1.IsCapableOfWholeWorkType(work))
                                currentPawn._val1.workSettings.SetPriority(work, priorityToPercent._val1);
                        }
                        PrioritiesEncounteredCached.Add(priorityToPercent._val1);
                    }
                    //set remaining pawns to 0
                    if(pawnsIterated < jobsCount.Count)
                    {
                        for(; pawnsIterated < jobsCount.Count; pawnsIterated++)
                        {
                            if(!jobsCount[pawnsIterated]._val1.IsCapableOfWholeWorkType(work))
                                continue;
                            jobsCount[pawnsIterated]._val1.workSettings.SetPriority(work, 0);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                ExceptionUtil.LogAllInnerExceptions(e);
            }

            void FillListOfPriorityToPercentOfColonists(WorkTypeDef work, List<Tuple2<int, float>> toFill)
            {
                toFill.Clear();
                foreach(var priority in PawnsData.PriorityToWorkTypesAndPercentOfPawns)
                    toFill.Add(new Tuple2<int, float>(priority._val1, priority._val2[work]));
                toFill.Sort((x, y) => x._val1.CompareTo(y._val1));
            }
        }
    }
}
