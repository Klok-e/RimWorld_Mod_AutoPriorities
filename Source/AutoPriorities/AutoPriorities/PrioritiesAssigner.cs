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
        private List<(int priority, double percent)> PriorityPercentCached { get; } =
            new List<(int priority, double percent)>();

        private Dictionary<Pawn, Dictionary<WorkTypeDef, Priority>> PawnJobsCached { get; } =
            new Dictionary<Pawn, Dictionary<WorkTypeDef, Priority>>();


        public PrioritiesAssigner()
        {
        }

        public void AssignPriorities(PawnsData pawnsData)
        {
            try
            {
                PawnJobsCached.Clear();
                foreach (var pawn in pawnsData.AllPlayerPawns)
                    PawnJobsCached.Add(pawn, new Dictionary<WorkTypeDef, Priority>());

#if DEBUG
                Controller.Log!.Message($"skilled");
#endif
                // assign skilled jobs
                AssignJobs(pawnsData, PawnJobsCached,
                    pawnsData.WorkTypes
                        .Where(work => !pawnsData.WorkTypesNotRequiringSkills.Contains(work)),
                    work => pawnsData.SortedPawnFitnessForEveryWork[work]);

#if DEBUG
                Controller.Log!.Message($"not skilled");
#endif
                // assign non skilled jobs
                AssignJobs(pawnsData, PawnJobsCached,
                    pawnsData.WorkTypesNotRequiringSkills,
                    work => pawnsData.SortedPawnFitnessForEveryWork[work]
                        .Select(p => (p.pawn, 1d / (1 + PawnJobsCached[p.pawn].Count)))
                        .OrderByDescending(p => p.Item2)
                        .ToList());
            }
            catch (Exception e)
            {
                e.LogStackTrace();
            }
        }

        private void AssignJobs(PawnsData pawnsData,
            IDictionary<Pawn, Dictionary<WorkTypeDef, Priority>> pawnJobs,
            IEnumerable<WorkTypeDef> workTypes,
            Func<WorkTypeDef, List<(Pawn pawn, double fitness)>> fitnessGetter)
        {
            //skip works not requiring skills because they will be handled later
            foreach (var work in workTypes)
            {
                FillListPriorityPercents(pawnsData, work, PriorityPercentCached);

                var pawns = fitnessGetter(work);
#if DEBUG
                Controller.Log!.Message($"worktype {work.defName}");
#endif

                foreach (var (priority, jobsToSet) in PriorityPercentCached
                    .Distinct(x => x.priority)
                    .Select(a => a.percent)
                    .IterPercents(pawns.Count)
                    .GroupBy(v => v.percentIndex)
                    .Select(g => (PriorityPercentCached[g.Key].priority, g.Count()))
                    .OrderBy(x => x.priority))
                {
                    var jobsSet = 0;
                    // iterate over all the pawns for this job with current priority
                    for (var i = 0; i < pawns.Count && jobsSet < jobsToSet; i++)
                    {
                        var pawn = pawns[i].pawn;

                        if ( // if this job was already set, skip
                            pawnJobs[pawn].ContainsKey(work) ||
                            // count amount of jobs assigned to pawn on this priority, then compare with max
                            pawnsData.MaxJobsPawnPriority.TryGetValue(priority, out var maxJobCount) &&
                            pawnJobs[pawn].Count(
                                kv => kv.Value.V == priority)
                            >= maxJobCount.V)
                            continue;

                        pawnJobs[pawn][work] = priority;
                        jobsSet += 1;
                        pawn.workSettings.SetPriority(work, priority);
                    }
                }

                // set remaining to zero
                for (var i = 0; i < pawns.Count; i++)
                {
                    var pawn = pawns[i].pawn;

                    // if this job was already set, skip
                    if (pawnJobs[pawn].ContainsKey(work))
                        continue;

                    pawn.workSettings.SetPriority(work, 0);
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