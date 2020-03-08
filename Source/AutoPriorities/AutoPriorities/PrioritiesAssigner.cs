using AutoPriorities.Extensions;
using AutoPriorities.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
                var pawnJobCount = new Dictionary<Pawn, int>(pawnsData.AllPlayerPawns.Count);
                foreach (var item in pawnsData.AllPlayerPawns)
                    pawnJobCount.Add(item, 0);

                var priorityPercent = new List<(int priority, float percent)>();

                //skip works not requiring skills because they will be handled later
                foreach (var work in pawnsData.WorkTypes
                    .Where(work => !pawnsData.WorkTypesNotRequiringSkills.Contains(work)))
                {
                    FillListOfPriorityToPercentOfColonists(pawnsData, work, priorityPercent);

                    var pawns = pawnsData.SortedPawnFitnessForEveryWork[work];
                    float pawnsCount = pawns.Count;

                    PrioritiesEncounteredCached.Clear();
                    var pawnsIterated = 0;
                    var mustBeIteratedForThisPriority = 0f;
                    //skip repeating priorities
                    foreach (var (priority, percent) in priorityPercent
                        .Where(priorityToPercent => !PrioritiesEncounteredCached.Contains(priorityToPercent.priority)))
                    {
                        mustBeIteratedForThisPriority += percent * pawnsCount;
                        //Log.Message($"mustBeIteratedForThisPriority {priorityToPercent._val1}: {mustBeIteratedForThisPriority}; pawnsCount: {pawnsCount}");
                        for (; pawnsIterated < mustBeIteratedForThisPriority; pawnsIterated++)
                        {
                            var (pawn, _) = pawns[pawnsIterated];

                            //skip incapable pawns
                            if (!pawn.IsCapableOfWholeWorkType(work))
                                continue;

                            //Log.Message($"in loop mustBeIteratedForThisPriority {priorityToPercent._val1}: {mustBeIteratedForThisPriority}; pawnsIterated: {pawnsIterated}");
                            pawn.workSettings.SetPriority(work, priority);

                            pawnJobCount[pawn] += 1;
                        }

                        PrioritiesEncounteredCached.Add(priority);
                    }

                    //set remaining pawns to 0
                    if (pawnsIterated >= pawnsCount)
                        continue;

                    for (; pawnsIterated < pawnsCount; pawnsIterated++)
                    {
                        if (!pawns[pawnsIterated].pawn.IsCapableOfWholeWorkType(work))
                            continue;
                        pawns[pawnsIterated].pawn.workSettings.SetPriority(work, 0);
                    }
                }

                //turn dict to list
                var jobsCount = new List<(Pawn pawn, int count)>(pawnJobCount.Count);
                foreach (var item in pawnJobCount)
                    jobsCount.Add((item.Key, item.Value));

                //sort by ascending to then iterate (lower count of works assigned gets works first)
                jobsCount.Sort((x, y) => x.count.CompareTo(y.count));
                foreach (var work in pawnsData.WorkTypesNotRequiringSkills)
                {
                    FillListOfPriorityToPercentOfColonists(pawnsData, work, priorityPercent);

                    PrioritiesEncounteredCached.Clear();
                    int pawnsIterated = 0;
                    float mustBeIteratedForThisPriority = 0f;
                    foreach (var priorityToPercent in priorityPercent)
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

        private static void FillListOfPriorityToPercentOfColonists(PawnsData pawnsData, WorkTypeDef work,
            List<(int, float)> toFill)
        {
            toFill.Clear();
            toFill.AddRange(pawnsData.WorkTables.Select(priority => (priority.priority, priority.workTypes[work])));
            toFill.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        }
    }
}