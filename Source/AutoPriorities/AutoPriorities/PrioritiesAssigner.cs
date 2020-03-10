using AutoPriorities.Extensions;
using AutoPriorities.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using AutoPriorities.Core;
using Verse;

namespace AutoPriorities
{
    internal class PrioritiesAssigner
    {
        private List<(int priority, double percent)> PriorityPercentCached { get; }
        private Dictionary<Pawn, int> PawnJobCountCached { get; }

        public PrioritiesAssigner()
        {
            PriorityPercentCached = new List<(int priority, double percent)>();
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
                var covered = 0;

                //skip repeating priorities
                foreach (var (iter, priorityInd) in PriorityPercentCached
                    .Distinct(x => x.priority)
                    .Select(a => a.percent)
                    .IterPercents(pawns.Count))
                {
                    covered = iter;

                    var (priority, _) = PriorityPercentCached[priorityInd];
                    var (pawn, _) = pawns[iter];

                    //Controller.Log.Message(
                    //    $"iter {iter}, priority {priorityInd}, pawn {pawn.NameFullColored}, priority {priority}");

                    //skip incapable pawns
                    if (pawn.IsCapableOfWholeWorkType(work))
                    {
                        pawn.workSettings.SetPriority(work, priority);

                        pawnJobCount[pawn] += 1;
                    }
                }

                //set remaining pawns to 0
                for (var i = covered + 1; i < pawns.Count; i++)
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

                var covered = 0;

                //skip repeating priorities
                foreach (var (iter, percentIndex) in PriorityPercentCached
                    .Distinct(x => x.priority)
                    .Select(a => a.percent)
                    .IterPercents(jobsCount.Count))
                {
                    covered = iter;

                    var (priority, _) = PriorityPercentCached[percentIndex];
                    var (pawn, _) = jobsCount[iter];

                    //Controller.Log.Message(
                    //    $"iter {iter}, priority {percentIndex}, pawn {pawn.NameFullColored}, priority {priority}");

                    //skip incapable pawns
                    if (pawn.IsCapableOfWholeWorkType(work))
                        pawn.workSettings.SetPriority(work, priority);
                }

                //set remaining pawns to 0
                for (var i = covered + 1; i < jobsCount.Count; i++)
                {
                    if (!jobsCount[i].pawn.IsCapableOfWholeWorkType(work))
                        continue;
                    jobsCount[i].pawn.workSettings.SetPriority(work, 0);
                }
            }
        }

        private static void FillListPriorityPercents(PawnsData pawnsData, WorkTypeDef work,
            List<(int, double)> priorityPercents)
        {
            priorityPercents.Clear();
            priorityPercents.AddRange(pawnsData.WorkTables
                .Select(priority => (priority.priority, priority.workTypes[work].Value))
                .Where(a => a.priority > 0));
            priorityPercents.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        }
    }
}