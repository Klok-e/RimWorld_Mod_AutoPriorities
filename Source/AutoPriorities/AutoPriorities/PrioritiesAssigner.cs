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
                    FillListPriorityPercents(pawnsData, work, priorityPercent);

                    var pawns = pawnsData.SortedPawnFitnessForEveryWork[work];
                    var coveredPawns = (int) (pawns.Count * priorityPercent.Sum(a => a.percent));

                    PrioritiesEncounteredCached.Clear();
                    //skip repeating priorities
                    foreach (var (iter, priorityInd) in priorityPercent
                        .Where(priorityToPercent => !PrioritiesEncounteredCached.Contains(priorityToPercent.priority))
                        .Select(a => a.percent)
                        .IterPercents(coveredPawns))
                    {
                        var (priority, _) = priorityPercent[priorityInd];
                        var (pawn, _) = pawns[iter];

                        //skip incapable pawns
                        if (pawn.IsCapableOfWholeWorkType(work))
                        {
                            pawn.workSettings.SetPriority(work, priority);

                            pawnJobCount[pawn] += 1;
                        }

                        PrioritiesEncounteredCached.Add(priority);
                    }

                    //set remaining pawns to 0
                    for (var i = coveredPawns; i < pawns.Count; i++)
                    {
                        if (!pawns[i].pawn.IsCapableOfWholeWorkType(work))
                            continue;
                        pawns[i].pawn.workSettings.SetPriority(work, 0);
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
                    FillListPriorityPercents(pawnsData, work, priorityPercent);

                    PrioritiesEncounteredCached.Clear();
                    var coveredPawns = (int) (jobsCount.Count * priorityPercent.Sum(a => a.percent));

                    //skip repeating priorities
                    foreach (var (iter, percentIndex) in priorityPercent
                        .Where(priorityToPercent => !PrioritiesEncounteredCached.Contains(priorityToPercent.priority))
                        .Select(a => a.percent)
                        .IterPercents(coveredPawns))
                    {
                        var (priority, _) = priorityPercent[percentIndex];
                        var (pawn, _) = jobsCount[iter];

                        //skip incapable pawns
                        if (pawn.IsCapableOfWholeWorkType(work))
                            pawn.workSettings.SetPriority(work, priority);

                        PrioritiesEncounteredCached.Add(priority);
                    }

                    //set remaining pawns to 0
                    for (var i = coveredPawns; i < jobsCount.Count; i++)
                    {
                        if (!jobsCount[i].pawn.IsCapableOfWholeWorkType(work))
                            continue;
                        jobsCount[i].pawn.workSettings.SetPriority(work, 0);
                    }
                }
            }
            catch (Exception e)
            {
                ExceptionUtil.LogAllInnerExceptions(e);
            }
        }

        private static void FillListPriorityPercents(PawnsData pawnsData, WorkTypeDef work,
            List<(int, float)> priorityPercents)
        {
            priorityPercents.Clear();
            priorityPercents.AddRange(pawnsData.WorkTables.Select(priority =>
                (priority.priority, priority.workTypes[work])));
            priorityPercents.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        }
    }
}