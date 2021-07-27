using System;
using System.Collections.Generic;
using System.Linq;
using AutoPriorities.APLogger;
using AutoPriorities.Core;
using AutoPriorities.Extensions;
using AutoPriorities.WorldInfoRetriever;
using AutoPriorities.Wrappers;

namespace AutoPriorities
{
    public class PrioritiesAssigner
    {
        private readonly ILogger _logger;
        private readonly PawnsData _pawnsData;
        private readonly IWorldInfoFacade _worldInfo;

        public PrioritiesAssigner(IWorldInfoFacade worldInfo, PawnsData pawnsData, ILogger logger)
        {
            _worldInfo = worldInfo;
            _pawnsData = pawnsData;
            _logger = logger;
        }

        private List<(Priority priority, JobCount maxJobs, double percent)> PriorityPercentCached { get; } =
            new();

        private Dictionary<IPawnWrapper, Dictionary<IWorkTypeWrapper, Priority>> PawnJobsCached { get; } =
            new();

        public void AssignPriorities()
        {
            try
            {
                PawnJobsCached.Clear();
                foreach (var pawn in _pawnsData.AllPlayerPawns)
                    PawnJobsCached.Add(pawn, new Dictionary<IWorkTypeWrapper, Priority>());

#if DEBUG
                Controller.Log!.Message("important");
#endif
                // assign `important` jobs because hardcoding is easy
                var importantWorks = new[] {"Firefighter", "Patient", "PatientBedRest", "BasicWorker"}
                                     .Select(_worldInfo.StringToDef)
                                     .Where(def => def is not null)
                                     .Select(x => x!)
                                     .ToHashSet();
                AssignJobs(_pawnsData, PawnJobsCached,
                    importantWorks,
                    work => _pawnsData.SortedPawnFitnessForEveryWork[work]);

#if DEBUG
                Controller.Log!.Message("skilled");
#endif
                // assign skilled jobs except important jobs
                AssignJobs(_pawnsData, PawnJobsCached,
                    _pawnsData.WorkTypes
                              .Subtract(importantWorks)
                              .Where(work => !_pawnsData.WorkTypesNotRequiringSkills.Contains(work)),
                    work => _pawnsData.SortedPawnFitnessForEveryWork[work]);

#if DEBUG
                Controller.Log!.Message("non skilled");
#endif
                // assign non skilled jobs except important jobs
                AssignJobs(_pawnsData, PawnJobsCached,
                    _pawnsData.WorkTypesNotRequiringSkills
                              .Subtract(importantWorks),
                    work => _pawnsData.SortedPawnFitnessForEveryWork[work]
                                      .Select(p => (p.pawn, 1d / (1 + PawnJobsCached[p.pawn]
                                          .Count)))
                                      .OrderByDescending(p => p.Item2)
                                      .ToList());
            }
            catch (Exception e)
            {
                _logger.Err(e);
            }
        }

        private void AssignJobs(PawnsData pawnsData,
            IDictionary<IPawnWrapper, Dictionary<IWorkTypeWrapper, Priority>> pawnJobs,
            IEnumerable<IWorkTypeWrapper> workTypes,
            Func<IWorkTypeWrapper, List<(IPawnWrapper pawn, double fitness)>> fitnessGetter)
        {
            //skip works not requiring skills because they will be handled later
            foreach (var work in workTypes)
            {
                FillListPriorityPercents(pawnsData, work, PriorityPercentCached);

                var pawns = fitnessGetter(work);
#if DEBUG
                _logger.Info($"worktype {work.defName}");
#endif

                foreach (var (priority, maxJobs, jobsToSet) in PriorityPercentCached
                                                               .Distinct(x => x.priority)
                                                               .Select(a => a.percent)
                                                               .IterPercents(pawns.Count)
                                                               .GroupBy(v => v.percentIndex)
                                                               .Select(g =>
                                                                   (PriorityPercentCached[g.Key]
                                                                       .priority, PriorityPercentCached[g.Key]
                                                                       .maxJobs, g.Count()))
                                                               .OrderBy(x => x.priority.V))
                {
                    var jobsSet = 0;
                    // iterate over all the pawns for this job with current priority
                    for (var i = 0; i < pawns.Count && jobsSet < jobsToSet; i++)
                    {
                        var pawn = pawns[i]
                            .pawn;

                        if ( // if this job was already set, skip
                            pawnJobs[pawn]
                                .ContainsKey(work) ||
                            // count amount of jobs assigned to pawn on this priority, then compare with max
                            pawnJobs[pawn]
                                .Count(
                                    kv => kv.Value.V == priority.V)
                            >= maxJobs.V)
                            continue;

                        pawnJobs[pawn][work] = priority;
                        jobsSet += 1;
                        pawn.workSettingsSetPriority(work, priority.V);
                    }
                }

                // set remaining to zero
                for (var i = 0; i < pawns.Count; i++)
                {
                    var pawn = pawns[i]
                        .pawn;

                    // if this job was already set, skip
                    if (pawnJobs[pawn]
                        .ContainsKey(work))
                        continue;

                    pawn.workSettingsSetPriority(work, 0);
                }
            }
        }

        private static void FillListPriorityPercents(PawnsData pawnsData,
            IWorkTypeWrapper work,
            List<(Priority, JobCount, double)> priorities)
        {
            priorities.Clear();
            priorities.AddRange(pawnsData.WorkTables
                                         .Select(tup => (tup.priority, tup.maxJobs, tup.workTypes[work]
                                             .Value))
                                         .Distinct(t => t.priority)
                                         .Where(t => t.priority.V > 0));
            priorities.Sort((x, y) => x.Item1.V.CompareTo(y.Item1.V));
        }
    }
}
