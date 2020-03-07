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

        public PrioritiesAssigner()
        {
            PrioritiesEncounteredCached = new HashSet<int>();
        }

        public void AssignPriorities(PawnsData pawnsData)
        {
            try
            {
                var listOfPawnAndAmountOfJobsAssigned = new Dictionary<Pawn, int>(pawnsData.AllPlayerPawns.Count);
                foreach (var item in pawnsData.AllPlayerPawns)
                    listOfPawnAndAmountOfJobsAssigned.Add(item, 0);

                var priorityToPercentOfColonists = new List<(int priority, float percent)>();
                foreach (var work in pawnsData.WorkTypes)
                {
                    //skip works not requiring skills because they will be handled later
                    if (pawnsData.WorkTypesNotRequiringSkills.Contains(work))
                        continue;

                    FillListOfPriorityToPercentOfColonists(pawnsData, work, priorityToPercentOfColonists);

                    var pawns = pawnsData.SortedPawnFitnessForEveryWork[work];
                    float pawnsCount = pawns.Count;

                    PrioritiesEncounteredCached.Clear();
                    int pawnsIterated = 0;
                    float mustBeIteratedForThisPriority = 0f;
                    foreach (var priorityToPercent in priorityToPercentOfColonists)
                    {
                        //skip repeating priorities
                        if (PrioritiesEncounteredCached.Contains(priorityToPercent.priority))
                            continue;

                        mustBeIteratedForThisPriority += priorityToPercent.percent * pawnsCount;
                        //Log.Message($"mustBeIteratedForThisPriority {priorityToPercent._val1}: {mustBeIteratedForThisPriority}; pawnsCount: {pawnsCount}");
                        for (; pawnsIterated < mustBeIteratedForThisPriority; pawnsIterated++)
                        {
                            var currentPawn = pawns[pawnsIterated];

                            //skip incapable pawns
                            if (currentPawn.pawn.IsCapableOfWholeWorkType(work))
                            {
                                //Log.Message($"in loop mustBeIteratedForThisPriority {priorityToPercent._val1}: {mustBeIteratedForThisPriority}; pawnsIterated: {pawnsIterated}");
                                currentPawn.pawn.workSettings.SetPriority(work, priorityToPercent.priority);

                                listOfPawnAndAmountOfJobsAssigned[currentPawn.pawn] += 1;
                            }
                        }

                        PrioritiesEncounteredCached.Add(priorityToPercent.priority);
                    }

                    //set remaining pawns to 0
                    if (pawnsIterated < pawnsCount)
                    {
                        for (; pawnsIterated < pawnsCount; pawnsIterated++)
                        {
                            if (!pawns[pawnsIterated].pawn.IsCapableOfWholeWorkType(work))
                                continue;
                            pawns[pawnsIterated].pawn.workSettings.SetPriority(work, 0);
                        }
                    }
                }

                //turn dict to list
                var jobsCount = new List<(Pawn pawn, int count)>(listOfPawnAndAmountOfJobsAssigned.Count);
                foreach (var item in listOfPawnAndAmountOfJobsAssigned)
                    jobsCount.Add((item.Key, item.Value));

                //sort by ascending to then iterate (lower count of works assigned gets works first)
                jobsCount.Sort((x, y) => x.count.CompareTo(y.count));
                foreach (var work in pawnsData.WorkTypesNotRequiringSkills)
                {
                    FillListOfPriorityToPercentOfColonists(pawnsData, work, priorityToPercentOfColonists);

                    PrioritiesEncounteredCached.Clear();
                    int pawnsIterated = 0;
                    float mustBeIteratedForThisPriority = 0f;
                    foreach (var priorityToPercent in priorityToPercentOfColonists)
                    {
                        //skip repeating priorities
                        if (PrioritiesEncounteredCached.Contains(priorityToPercent.priority))
                            continue;

                        mustBeIteratedForThisPriority += priorityToPercent.percent * jobsCount.Count;
                        for (; pawnsIterated < mustBeIteratedForThisPriority; pawnsIterated++)
                        {
                            var currentPawn = jobsCount[pawnsIterated];

                            //skip incapable pawns
                            if (currentPawn.pawn.IsCapableOfWholeWorkType(work))
                                currentPawn.pawn.workSettings.SetPriority(work, priorityToPercent.priority);
                        }

                        PrioritiesEncounteredCached.Add(priorityToPercent.priority);
                    }

                    //set remaining pawns to 0
                    if (pawnsIterated < jobsCount.Count)
                    {
                        for (; pawnsIterated < jobsCount.Count; pawnsIterated++)
                        {
                            if (!jobsCount[pawnsIterated].pawn.IsCapableOfWholeWorkType(work))
                                continue;
                            jobsCount[pawnsIterated].pawn.workSettings.SetPriority(work, 0);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ExceptionUtil.LogAllInnerExceptions(e);
            }
        }

        static void FillListOfPriorityToPercentOfColonists(PawnsData pawnsData, WorkTypeDef work,
            List<(int, float)> toFill)
        {
            toFill.Clear();
            foreach (var priority in pawnsData.WorkTables)
                toFill.Add((priority.priority, priority.workTypes[work]));
            toFill.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        }
    }
}