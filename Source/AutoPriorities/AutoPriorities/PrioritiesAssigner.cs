using System;
using System.Collections.Generic;
using System.Linq;
using AutoPriorities.APLogger;
using AutoPriorities.Core;
using AutoPriorities.Extensions;
using AutoPriorities.ImportantJobs;
using AutoPriorities.WorldInfoRetriever;
using AutoPriorities.Wrappers;

namespace AutoPriorities
{
    public class PrioritiesAssigner
    {
        private readonly IImportantJobsProvider _importantJobsProvider;
        private readonly ILogger _logger;
        private readonly PawnsData _pawnsData;
        private readonly IWorldInfoRetriever _worldInfoRetriever;

        public PrioritiesAssigner(
            PawnsData pawnsData,
            ILogger logger,
            IImportantJobsProvider importantJobsProvider,
            IWorldInfoRetriever worldInfoRetriever)
        {
            _pawnsData = pawnsData;
            _logger = logger;
            _importantJobsProvider = importantJobsProvider;
            _worldInfoRetriever = worldInfoRetriever;
        }

        private List<(Priority priority, JobCount maxJobs, double percent)> PriorityPercentCached { get; } = new();

        private Dictionary<IPawnWrapper, Dictionary<IWorkTypeWrapper, Priority>> PawnJobsCached { get; } = new();

        public void AssignPriorities()
        {
            try
            {
                PawnJobsCached.Clear();
                foreach (var pawn in _pawnsData.CurrentMapPlayerPawns)
                    PawnJobsCached.Add(pawn, new Dictionary<IWorkTypeWrapper, Priority>());

#if DEBUG
                _logger.Info("important");
#endif
                // assign `important` jobs because hardcoding is easy
                var importantWorks = _importantJobsProvider.ImportantWorkTypes();
                AssignJobs(
                    _pawnsData,
                    PawnJobsCached,
                    importantWorks,
                    work => _pawnsData.SortedPawnFitnessForEveryWork[work]);

#if DEBUG
                _logger.Info("skilled");
#endif
                // assign skilled jobs except important jobs
                AssignJobs(
                    _pawnsData,
                    PawnJobsCached,
                    _pawnsData.WorkTypes.Subtract(importantWorks)
                        .Where(work => !_pawnsData.WorkTypesNotRequiringSkills.Contains(work)),
                    work => _pawnsData.SortedPawnFitnessForEveryWork[work],
                    _worldInfoRetriever.GetMinimumWorkFitness());

#if DEBUG
                _logger.Info("non skilled");
#endif
                // assign non skilled jobs except important jobs
                AssignJobs(
                    _pawnsData,
                    PawnJobsCached,
                    _pawnsData.WorkTypesNotRequiringSkills.Subtract(importantWorks),
                    work => _pawnsData.SortedPawnFitnessForEveryWork[work]
                        .Select(
                            p => (p.pawn, 1d / (1 + PawnJobsCached[p.pawn]
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
            Func<IWorkTypeWrapper, List<(IPawnWrapper pawn, double fitness)>> fitnessGetter,
            double? minimumFitness = null)
        {
            foreach (var work in workTypes)
            {
                FillListPriorityPercents(pawnsData, work, PriorityPercentCached);

                var pawns = fitnessGetter(work);
#if DEBUG
                _logger.Info($"worktype {work.DefName}");
#endif

#if DEBUG
                foreach (var (pawn, fitness) in pawns) _logger.Info($"pawn {pawn.NameFullColored}; fitness {fitness}");
#endif

                foreach (var (priority, maxJobs, jobsToSet) in PriorityPercentCached
                             .Distinct(x => x.priority)
                             .Select(a => a.percent)
                             .IterPercents(pawns.Count)
                             .GroupBy(v => v.percentIndex)
                             .Select(
                                 g => (PriorityPercentCached[g.Key]
                                     .priority, PriorityPercentCached[g.Key]
                                     .maxJobs, g.Count()))
                             .OrderBy(x => x.priority.v))
                {
                    var jobsSet = 0;
                    // iterate over all the pawns for this job with current priority
                    foreach (var (pawn, fitness) in pawns)
                    {
                        if (jobsSet >= jobsToSet)
                            break;

                        if ( // if this job was already set, skip
                            pawnJobs[pawn]
                                .ContainsKey(work) ||
                            // count amount of jobs assigned to pawn on this priority, then compare with max
                            pawnJobs[pawn]
                                .Count(kv => kv.Value.v == priority.v) >= maxJobs.v)
                            continue;

                        if (fitness < minimumFitness)
                        {
#if DEBUG
                            _logger.Info($"fitness < minimumFitness: {fitness} < {minimumFitness}");
#endif
                            continue;
                        }

                        pawnJobs[pawn][work] = priority;
                        jobsSet += 1;
                        pawn.WorkSettingsSetPriority(work, priority.v);
                    }
                }

                // set remaining to zero
                foreach (var (pawn, _) in pawns)
                {
                    // if this job was already set, skip
                    if (pawnJobs[pawn]
                        .ContainsKey(work))
                        continue;

                    pawn.WorkSettingsSetPriority(work, 0);
                }
            }
        }

        private void FillListPriorityPercents(PawnsData pawnsData,
            IWorkTypeWrapper work,
            List<(Priority, JobCount, double)> priorities)
        {
            priorities.Clear();
            priorities.AddRange(
                pawnsData.WorkTables.Select(
                        tup => (priority:
                            tup.Priority,
                            jobCount: tup.JobCount,
                            _pawnsData.PercentValue(tup.WorkTypes[work], work, tup.Priority)))
                    .Distinct(t => t.priority)
                    .Where(t => t.priority.v > 0));
            priorities.Sort((x, y) => x.Item1.v.CompareTo(y.Item1.v));
        }
    }
}
