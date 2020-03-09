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
        private List<(int priority, float percent)> PriorityPercentCached { get; }
        private Dictionary<Pawn, int> PawnJobCountCached { get; }

        public PrioritiesAssigner()
        {
            PrioritiesEncounteredCached = new HashSet<int>();
            PriorityPercentCached = new List<(int priority, float percent)>();
            PawnJobCountCached = new Dictionary<Pawn, int>();
        }

        public void AssignPriorities(PawnsData pawnsData)
        {
            try
            {
                PawnJobCountCached.Clear();
                foreach (var item in pawnsData.AllPlayerPawns)
                    PawnJobCountCached.Add(item, 0);

                AssignSkilledJobs(pawnsData, PawnJobCountCached);
                AssignNonSkilledJobs(pawnsData, PawnJobCountCached);
            }
            catch (Exception e)
            {
                ExceptionUtil.LogAllInnerExceptions(e);
            }
        }

        private void AssignSkilledJobs(PawnsData pawnsData, IDictionary<Pawn, int> pawnJobCount)
        {
            //skip works not requiring skills because they will be handled later
            foreach (var work in pawnsData.WorkTypes
                .Where(work => !pawnsData.WorkTypesNotRequiringSkills.Contains(work)))
            {
                FillListPriorityPercents(pawnsData, work, PriorityPercentCached);

                var pawns = pawnsData.SortedPawnFitnessForEveryWork[work];
                var coveredPawns = (int) (pawns.Count * PriorityPercentCached.Sum(a => a.percent));

                PrioritiesEncounteredCached.Clear();
                //skip repeating priorities
                foreach (var (iter, priorityInd) in PriorityPercentCached
                    .Where(priorityToPercent => !PrioritiesEncounteredCached.Contains(priorityToPercent.priority))
                    .Select(a => a.percent)
                    .IterPercents(coveredPawns))
                {
                    var (priority, _) = PriorityPercentCached[priorityInd];
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
        }

        private void AssignNonSkilledJobs(PawnsData pawnsData, Dictionary<Pawn, int> pawnJobCount)
        {
            //turn dict to list
            List<(Pawn pawn, int count)> jobsCount = pawnJobCount.Select(item => (item.Key, item.Value)).ToList();

            //sort by ascending to then iterate (lower count of works assigned gets works first)
            jobsCount.Sort((x, y) => x.count.CompareTo(y.count));
            foreach (var work in pawnsData.WorkTypesNotRequiringSkills)
            {
                FillListPriorityPercents(pawnsData, work, PriorityPercentCached);

                PrioritiesEncounteredCached.Clear();
                var coveredPawns = (int) (jobsCount.Count * PriorityPercentCached.Sum(a => a.percent));

                //skip repeating priorities
                foreach (var (iter, percentIndex) in PriorityPercentCached
                    .Where(priorityToPercent => !PrioritiesEncounteredCached.Contains(priorityToPercent.priority))
                    .Select(a => a.percent)
                    .IterPercents(coveredPawns))
                {
                    var (priority, _) = PriorityPercentCached[percentIndex];
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

        private static void FillListPriorityPercents(PawnsData pawnsData, WorkTypeDef work,
            List<(int, float)> priorityPercents)
        {
            priorityPercents.Clear();
            priorityPercents.AddRange(pawnsData.WorkTables
                .Select(priority => (priority.priority, priority.workTypes[work].Value))
                .Where(a => a.priority > 0));
            priorityPercents.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        }
    }
}