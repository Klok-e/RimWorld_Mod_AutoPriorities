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
                foreach (var pawn in pawnsData.AllPlayerPawns)
                    PawnJobCountCached.Add(pawn, 0);

                AssignSkilledJobs(pawnsData, PawnJobCountCached);
                AssignNonSkilledJobs(pawnsData, PawnJobCountCached);
            }
            catch (Exception e)
            {
                e.LogStackTrace();
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
                var covered = -1;
#if DEBUG
                Controller.Log!.Message($"skilled worktype {work.defName}");
#endif

                //skip repeating priorities
                foreach (var (iter, priorityInd) in PriorityPercentCached
                    .Distinct(x => x.priority)
                    .Select(a => a.percent)
                    .IterPercents(pawns.Count))
                {
                    covered = iter;

                    var (priority, _) = PriorityPercentCached[priorityInd];
                    var (pawn, _) = pawns[iter];

#if DEBUG
                    Controller.Log.Message(
                        $"iter {iter}, priority index {priorityInd}, pawn {pawn.NameFullColored}, priority {priority}");
#endif

                    //skip incapable pawns
                    if (!pawn.IsCapableOfWholeWorkType(work))
                        continue;
                    pawn.workSettings.SetPriority(work, priority);

                    pawnJobCount[pawn] += 1;
                }

                //set remaining pawns to 0
                for (var i = covered + 1; i < pawns.Count; i++)
                {
#if DEBUG
                    //Controller.Log.Message($"iter {i}, pawn {pawns[i].pawn.NameFullColored} priority 0");
#endif
                    if (!pawns[i].pawn.IsCapableOfWholeWorkType(work))
                        continue;

                    pawns[i].pawn.workSettings.SetPriority(work, 0);
                }
            }
        }

        private void AssignNonSkilledJobs(PawnsData pawnsData, Dictionary<Pawn, int> pawnJobCount)
        {
            foreach (var work in pawnsData.WorkTypesNotRequiringSkills)
            {
                FillListPriorityPercents(pawnsData, work, PriorityPercentCached);

                // combine fitness and job count parameters
                // list of pawns and new fitness (higher is better, gets job faster)
                List<(Pawn pawn, double fitness)> pawns = pawnsData.SortedPawnFitnessForEveryWork[work]
                    .Select(p => (p.pawn, p.fitness >= 0d ? 1d / (1 + pawnJobCount[p.pawn]) : 0d)).ToList();
                
                // sort descending based on fitness
                pawns.Sort((x, y) => y.fitness.CompareTo(x.fitness));

                var covered = -1;

#if DEBUG
                Controller.Log!.Message($"unskilled worktype {work.defName}");
#endif

                //skip repeating priorities
                foreach (var (iter, percentIndex) in PriorityPercentCached
                    .Distinct(x => x.priority)
                    .Select(a => a.percent)
                    .IterPercents(pawns.Count))
                {
                    covered = iter;

                    var (priority, _) = PriorityPercentCached[percentIndex];
                    var (pawn, _) = pawns[iter];

#if DEBUG
                    Controller.Log.Message(
                        $"iter {iter}, priority {percentIndex}, pawn {pawn.NameFullColored}, priority {priority}");
#endif

                    //skip incapable pawns
                    if (pawn.IsCapableOfWholeWorkType(work))
                        pawn.workSettings.SetPriority(work, priority);
                }

                //set remaining pawns to 0
                for (var i = covered + 1; i < pawns.Count; i++)
                {
#if DEBUG
                    //Controller.Log.Message($"iter {i}, pawn {jobsCount[i].pawn.NameFullColored} priority 0");
#endif
                    if (pawns[i].pawn.IsCapableOfWholeWorkType(work))
                        pawns[i].pawn.workSettings.SetPriority(work, 0);
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