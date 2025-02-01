using System;
using System.Collections.Generic;
using System.Linq;
using AutoPriorities.APLogger;
using AutoPriorities.Core;
using AutoPriorities.Extensions;
using AutoPriorities.ImportantJobs;
using AutoPriorities.Wrappers;

namespace AutoPriorities
{
    public class PrioritiesAssigner
    {
        private readonly IImportantJobsProvider _importantJobsProvider;
        private readonly ILogger _logger;
        private readonly PawnsData _pawnsData;

        public PrioritiesAssigner(PawnsData pawnsData, ILogger logger, IImportantJobsProvider importantJobsProvider)
        {
            _pawnsData = pawnsData;
            _logger = logger;
            _importantJobsProvider = importantJobsProvider;
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
                AssignJobs(_pawnsData, PawnJobsCached, importantWorks, work => _pawnsData.SortedPawnFitnessForEveryWork[work]);

#if DEBUG
                _logger.Info("skilled");
#endif
                // assign skilled jobs except important jobs
                AssignJobs(
                    _pawnsData,
                    PawnJobsCached,
                    _pawnsData.WorkTypes.Subtract(importantWorks).Where(work => !_pawnsData.WorkTypesNotRequiringSkills.Contains(work)),
                    work => _pawnsData.SortedPawnFitnessForEveryWork[work],
                    _pawnsData.MinimumSkillLevel);

#if DEBUG
                _logger.Info("non skilled");
#endif
                // assign non skilled jobs except important jobs
                AssignJobs(
                    _pawnsData,
                    PawnJobsCached,
                    _pawnsData.WorkTypesNotRequiringSkills.Subtract(importantWorks),
                    work => _pawnsData.SortedPawnFitnessForEveryWork[work]
                        .Select(p => new PawnFitnessData { Pawn = p.Pawn, Fitness = 1d / (1 + PawnJobsCached[p.Pawn].Count), SkillLevel = 0 })
                        .OrderByDescending(p => p.Fitness)
                        .ToList());
            }
            catch (Exception e)
            {
                _logger.Err(e);
            }
        }

        public void AssignPrioritiesSmarter()
        {
            // Setup the solver decision variables (cost) for each (workType, pawn, priority + not_assigned)
            var workTableEntries = _pawnsData.WorkTables.Distinct(y => y.Priority.v).ToArray();
            var assignmentOffsets = new Dictionary<(IWorkTypeWrapper, IPawnWrapper), int>();

            var variables = new List<double>();
            var bndl = new List<double>();
            var bndu = new List<double>();

            var index = 0;
            foreach (var workTypeKv in _pawnsData.SortedPawnFitnessForEveryWork)
            foreach (var pfd in workTypeKv.Value)
            {
                // Negative cost for real priorities => solver maximizes fitness
                for (var workTableIndex = 0; workTableIndex < workTableEntries.Length; workTableIndex++)
                {
                    var priority = workTableEntries[workTableIndex].Priority.v;

                    variables.Add(-pfd.Fitness / priority);
                    bndl.Add(0.0);
                    bndu.Add(1.0);
                }

                // Extra slot for "not assigned"
                variables.Add(0.0);
                bndl.Add(0.0);
                bndu.Add(1.0);
                assignmentOffsets[(workTypeKv.Key, pfd.Pawn)] = index;
                index += workTableEntries.Length + 1;
            }

            alglib.minlpcreate(variables.Count, out var state);

            alglib.minlpsetcost(state, variables.ToArray());

            // Forbid real priorities if pawn is opposed or skill too low
            index = 0;
            foreach (var workTypeKv in _pawnsData.SortedPawnFitnessForEveryWork)
            foreach (var pfd in workTypeKv.Value)
            {
                if ((pfd.IsOpposed && !_pawnsData.IgnoreOppositionToWork) || pfd.SkillLevel < _pawnsData.MinimumSkillLevel)
                    for (var workTableIndex = 0; workTableIndex < workTableEntries.Length; workTableIndex++)
                        bndu[index + workTableIndex] = 0.0;

                index += workTableEntries.Length + 1;
            }

            alglib.minlpsetbc(state, bndl.ToArray(), bndu.ToArray());

            // Exactly one choice (some priority or "not assigned") for each (workType, pawn)
            var sumIndex = 0;
            foreach (var workTypeKv in _pawnsData.SortedPawnFitnessForEveryWork)
            foreach (var pfd in workTypeKv.Value)
            {
                var constraintRow = new double[variables.Count];
                for (var workTableIndex = 0; workTableIndex < workTableEntries.Length + 1; workTableIndex++)
                    constraintRow[sumIndex + workTableIndex] = 1.0;

                alglib.minlpaddlc2dense(state, constraintRow, 1, 1);
                sumIndex += workTableEntries.Length + 1;
            }

            // Enforce exact jobsToSet for each workType+priority
            foreach (var workTypeKv in _pawnsData.SortedPawnFitnessForEveryWork)
            {
                PriorityPercentCached.Clear();
                FillListPriorityPercents(_pawnsData, workTypeKv.Key, PriorityPercentCached);
                var pawns = workTypeKv.Value;
                if (PriorityPercentCached.Count == 0) continue;

                var groups = PriorityPercentCached.Distinct(x => x.priority)
                    .Select(a => a.percent)
                    .IterPercents(pawns.Count)
                    .GroupBy(v => v.percentIndex)
                    .Select(g => (PriorityPercentCached[g.Key].priority, PriorityPercentCached[g.Key].maxJobs, jobsToSet: g.Count()))
                    .OrderBy(x => x.priority.v);

                foreach (var (priority, maxJobsStruct, jobsToSet) in groups)
                {
                    var constraintRow = new double[variables.Count];
                    foreach (var pfd in pawns)
                    {
                        var offset = assignmentOffsets[(workTypeKv.Key, pfd.Pawn)];
                        constraintRow[offset + (priority.v - 1)] = 1.0;
                    }

                    alglib.minlpaddlc2dense(state, constraintRow, jobsToSet, jobsToSet);
                }
            }

            // Each pawn can have up to maxJobs across all workTypes at a given priority
            for (var workTableIndex = 0; workTableIndex < workTableEntries.Length; workTableIndex++)
            {
                var maxJobs = workTableEntries[workTableIndex].JobCount.v;
                if (maxJobs <= 0) continue;

                foreach (var (wType, pawns) in _pawnsData.SortedPawnFitnessForEveryWork)
                {
                    var rowPawn = new double[variables.Count];
                    foreach (var pawnFitness in pawns)
                    {
                        var offset = assignmentOffsets[(wType, pawnFitness.Pawn)];
                        rowPawn[offset + workTableIndex] = 1.0;
                    }

                    alglib.minlpaddlc2dense(state, rowPawn, 0, maxJobs);
                }
            }

            alglib.minlpsetscale(state, Enumerable.Repeat(1.0, variables.Count).ToArray());

            alglib.minlpsetalgoipm(state);
            alglib.minlpoptimize(state);
            alglib.minlpresults(state, out var solution, out var rep);

            foreach (var (workType, pawns) in _pawnsData.SortedPawnFitnessForEveryWork)
            foreach (var pawnFitness in pawns)
            {
                var ind = assignmentOffsets[(workType, pawnFitness.Pawn)];

                var pawnWorkTypeVariables = solution[ind..(ind + workTableEntries.Length + 1)];
                var chosenPriorityIndex = pawnWorkTypeVariables.ArgMax();

                pawnFitness.Pawn.WorkSettingsSetPriority(
                    workType,
                    chosenPriorityIndex == workTableEntries.Length ? 0 : workTableEntries[chosenPriorityIndex].Priority.v);
            }
        }

        private void AssignJobs(PawnsData pawnsData, IDictionary<IPawnWrapper, Dictionary<IWorkTypeWrapper, Priority>> pawnJobs,
            IEnumerable<IWorkTypeWrapper> workTypes, Func<IWorkTypeWrapper, List<PawnFitnessData>> fitnessGetter, double? minimumSkillLevel = null)
        {
            foreach (var work in workTypes)
            {
                FillListPriorityPercents(pawnsData, work, PriorityPercentCached);

                var pawns = fitnessGetter(work);
#if DEBUG
                _logger.Info($"worktype {work.DefName}");
#endif

#if DEBUG
                foreach (var (pawn, fitness, skillLevel, isOpposed) in pawns)
                    _logger.Info($"pawn {pawn.NameFullColored}; fitness {fitness}; skill {skillLevel}; isOpposed {isOpposed}");
#endif

                foreach (var (priority, maxJobs, jobsToSet) in PriorityPercentCached.Distinct(x => x.priority)
                             .Select(a => a.percent)
                             .IterPercents(pawns.Count)
                             .GroupBy(v => v.percentIndex)
                             .Select(g => (PriorityPercentCached[g.Key].priority, PriorityPercentCached[g.Key].maxJobs, g.Count()))
                             .OrderBy(x => x.priority.v))
                {
                    var jobsSet = 0;
                    // iterate over all the pawns for this job with current priority
                    foreach (var (pawn, fitness, skillLevel, isOpposed) in pawns)
                    {
                        if (jobsSet >= jobsToSet)
                            break;

                        var jobsPawnHasOnThisPriority = pawnJobs[pawn].Count(kv => kv.Value.v == priority.v);
#if DEBUG
                        _logger.Info(
                            $"pawn {pawn.NameFullColored}; fitness {fitness}; jobsSet {jobsSet}; "
                            + $"jobsToSet {jobsToSet}; priority {priority.v}; "
                            + $"jobsPawnHasOnThisPriority {jobsPawnHasOnThisPriority}; maxJobs {maxJobs.v}");
#endif

                        if (pawnJobs[pawn].ContainsKey(work)
                            || jobsPawnHasOnThisPriority >= maxJobs.v
                            || (isOpposed && !pawnsData.IgnoreOppositionToWork))
                            continue;

                        if (skillLevel < minimumSkillLevel)
                        {
#if DEBUG
                            _logger.Info($"skillLevel < minimumSkillLevel: {skillLevel} < {minimumSkillLevel}");
#endif
                            continue;
                        }

                        pawnJobs[pawn][work] = priority;
                        jobsSet += 1;
                        pawn.WorkSettingsSetPriority(work, priority.v);
                    }
                }

                // set remaining to zero
                foreach (var (pawn, _, _, _) in pawns)
                {
                    // if this job was already set, skip
                    if (pawnJobs[pawn].ContainsKey(work))
                        continue;

                    pawn.WorkSettingsSetPriority(work, 0);
                }
            }
        }

        private void FillListPriorityPercents(PawnsData pawnsData, IWorkTypeWrapper work, List<(Priority, JobCount, double)> priorities)
        {
            priorities.Clear();
            priorities.AddRange(
                pawnsData.WorkTables.Select(
                        tup => (priority: tup.Priority, jobCount: tup.JobCount, _pawnsData.PercentValue(tup.WorkTypes[work], work, tup.Priority)))
                    .Distinct(t => t.priority)
                    .Where(t => t.priority.v > 0));
            priorities.Sort((x, y) => x.Item1.v.CompareTo(y.Item1.v));
        }
    }
}
