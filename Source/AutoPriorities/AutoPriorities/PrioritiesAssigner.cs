using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AutoPriorities.Core;
using AutoPriorities.Extensions;
using AutoPriorities.ImportantJobs;
using AutoPriorities.SerializableSimpleData;
using AutoPriorities.Utils;
using AutoPriorities.WorldInfoRetriever;
using AutoPriorities.Wrappers;
using Verse;
using ILogger = AutoPriorities.APLogger.ILogger;

namespace AutoPriorities
{
    public class PrioritiesAssigner
    {
        private readonly IImportantJobsProvider _importantJobsProvider;
        private readonly ILogger _logger;
        private readonly PawnsData _pawnsData;
        private readonly IWorldInfoRetriever _worldInfoRetriever;

        private double[] _matrixResultCached = [];

        public PrioritiesAssigner(PawnsData pawnsData, ILogger logger, IImportantJobsProvider importantJobsProvider,
            IWorldInfoRetriever worldInfoRetriever)
        {
            _pawnsData = pawnsData;
            _logger = logger;
            _importantJobsProvider = importantJobsProvider;
            _worldInfoRetriever = worldInfoRetriever;
        }

        public void AssignPriorities()
        {
            try
            {
                var pawnJobsCached = new Dictionary<IPawnWrapper, Dictionary<IWorkTypeWrapper, Priority>>();
                foreach (var pawn in _pawnsData.CurrentMapPlayerPawns)
                    pawnJobsCached.Add(pawn, new Dictionary<IWorkTypeWrapper, Priority>());

                var priorityPercentCached = new List<(Priority priority, JobCount maxJobs, double percent)>();

#if DEBUG
                _logger.Info("important");
#endif
                // assign `important` jobs because hardcoding is easy
                var importantWorks = _importantJobsProvider.ImportantWorkTypes();
                AssignJobs(
                    _pawnsData,
                    pawnJobsCached,
                    importantWorks,
                    work => _pawnsData.SortedPawnFitnessForEveryWork[work],
                    priorityPercentCached
                );

#if DEBUG
                _logger.Info("skilled");
#endif
                // assign skilled jobs except important jobs
                AssignJobs(
                    _pawnsData,
                    pawnJobsCached,
                    _pawnsData.WorkTypes.Subtract(importantWorks).Where(work => !_pawnsData.WorkTypesNotRequiringSkills.Contains(work)),
                    work => _pawnsData.SortedPawnFitnessForEveryWork[work],
                    priorityPercentCached,
                    _pawnsData.MinimumSkillLevel
                );

#if DEBUG
                _logger.Info("non skilled");
#endif
                // assign non skilled jobs except important jobs
                AssignJobs(
                    _pawnsData,
                    pawnJobsCached,
                    _pawnsData.WorkTypesNotRequiringSkills.Subtract(importantWorks),
                    work => _pawnsData.SortedPawnFitnessForEveryWork[work]
                        .Select(
                            p => new PawnFitnessData { Pawn = p.Pawn, Fitness = 1f / (1 + pawnJobsCached[p.Pawn].Count), SkillLevel = 0 }
                        )
                        .OrderByDescending(p => p.Fitness)
                        .ToList(),
                    priorityPercentCached
                );
            }
            catch (Exception e)
            {
                _logger.Err(e);
            }
        }

        public void StartOptimizationTaskOfAssignPriorities(Action? onFinished = null, Action? onSolverFailed = null)
        {
            SaveTablesAndPawnsIfDebug();

            var pawnsDataCopy = _pawnsData.ShallowCopy();
            ThreadPool.QueueUserWorkItem(
                _ =>
                {
                    try
                    {
                        AssignPrioritiesByOptimization(pawnsDataCopy, onSolverFailed);
                    }
                    catch (Exception e)
                    {
                        _logger.Err($"{nameof(StartOptimizationTaskOfAssignPriorities)} failed: {e}");
                    }
                    finally
                    {
                        onFinished?.Invoke();
                    }
                }
            );
        }

        public void AssignPrioritiesByOptimization(PawnsData pawnsData, Action? onSolverFailed = null)
        {
            // Setup the solver decision variables (cost) for each (workType, pawn, priority + not_assigned)
            var workTableEntries = pawnsData.WorkTables.Distinct(y => y.Priority.v).ToArray();
            var assignmentOffsets = new Dictionary<(IWorkTypeWrapper, IPawnWrapper), int>();

            var variables = new List<float>();
            var bndl = new List<float>();
            var bndu = new List<float>();

            {
                var index = 0;
                foreach (var workTypeKv in pawnsData.SortedPawnFitnessForEveryWork)
                foreach (var pfd in workTypeKv.Value)
                {
                    // Negative cost for real priorities => solver maximizes fitness
                    for (var workTableIndex = 0; workTableIndex < workTableEntries.Length; workTableIndex++)
                    {
                        var priority = workTableEntries[workTableIndex].Priority.v;

                        variables.Add(-pfd.Fitness / priority);
                        bndl.Add(0.0f);
                        bndu.Add(1.0f);
                    }

                    // Extra slot for "not assigned"
                    variables.Add(0.0f);
                    bndl.Add(0.0f);
                    bndu.Add(1.0f);

                    assignmentOffsets[(workTypeKv.Key, pfd.Pawn)] = index;
                    index += workTableEntries.Length + 1;
                }
            }

            // Forbid real priorities if pawn is opposed or skill too low
            foreach (var workTypeKv in pawnsData.SortedPawnFitnessForEveryWork)
            {
                var pawnFitnessDatas = workTypeKv.Value;
                var workType = workTypeKv.Key;

                foreach (var pawnFitnessData in pawnFitnessDatas)
                {
                    if (!CanPawnBeAssigned(pawnFitnessData, pawnsData))
                    {
                        var offset = assignmentOffsets[(workType, pawnFitnessData.Pawn)];

                        // Forbid all *real* priorities (leave "not assigned" free)
                        for (var workTableIndex = 0; workTableIndex < workTableEntries.Length; workTableIndex++)
                        {
                            bndu[offset + workTableIndex] = 0.0f;
                        }
                    }
                }
            }

            var model = new LinearModel(variables.ToArray(), bndl.ToArray(), bndu.ToArray());

            // Exactly one choice (some priority or "not assigned") for each (workType, pawn)
            foreach (var workTypeKv in pawnsData.SortedPawnFitnessForEveryWork)
            {
                var pawnFitnessDatas = workTypeKv.Value;
                var workType = workTypeKv.Key;

                foreach (var pawnFitnessData in pawnFitnessDatas)
                {
                    var offset = assignmentOffsets[(workType, pawnFitnessData.Pawn)];

                    var constraintRow = new float[variables.Count];
                    for (var workTableIndex = 0; workTableIndex < workTableEntries.Length + 1; workTableIndex++)
                        constraintRow[offset + workTableIndex] = 1.0f;

                    // sum of real priorities + "not assigned" must be exactly 1
                    model.AddConstraint(constraintRow, 1, 1);
                }
            }

            var priorityPercentCached = new List<(Priority priority, JobCount maxJobs, double percent)>();

            // Enforce exact jobsToSet for each workType+priority
            foreach (var workTypeKv in pawnsData.SortedPawnFitnessForEveryWork)
            {
                var pawnData = workTypeKv.Value;
                var workType = workTypeKv.Key;

                priorityPercentCached.Clear();
                FillListPriorityPercents(pawnsData, workType, priorityPercentCached);
                if (priorityPercentCached.Count == 0) continue;

                var groups = priorityPercentCached.Distinct(x => x.priority)
                    .Select(a => a.percent)
                    .IterPercents(pawnData.Count(pawnFitnessData => CanPawnBeAssigned(pawnFitnessData, pawnsData)))
                    .GroupBy(v => v.percentIndex)
                    .Select(g => (priorityPercentCached[g.Key].priority, jobsToSet: g.Count()))
                    .OrderBy(x => x.priority.v);

                var sumJobsRemain = pawnData.Count;
                foreach (var (priority, jobsToSet) in groups)
                {
                    var workTableIndex = workTableEntries.FirstIndexOf(x => x.Priority.Equals(priority));

                    var constraintRow = new float[variables.Count];
                    foreach (var pfd in pawnData)
                    {
                        var offset = assignmentOffsets[(workType, pfd.Pawn)];
                        constraintRow[offset + workTableIndex] = 1.0f;
                    }

                    model.AddConstraint(constraintRow, jobsToSet, jobsToSet);

                    sumJobsRemain -= jobsToSet;
                }

                if (sumJobsRemain > 0)
                {
                    var unsetPriorityIndex = workTableEntries.Length;

                    var constraintRow = new float[variables.Count];
                    foreach (var pfd in pawnData)
                    {
                        var offset = assignmentOffsets[(workType, pfd.Pawn)];
                        constraintRow[offset + unsetPriorityIndex] = 1.0f;
                    }

                    model.AddConstraint(constraintRow, sumJobsRemain, sumJobsRemain);
                }
            }

            // Each pawn can have up to maxJobs across all workTypes at a given priority
            for (var workTableIndex = 0; workTableIndex < workTableEntries.Length; workTableIndex++)
            {
                var maxJobs = workTableEntries[workTableIndex].JobCount.v;
                if (maxJobs <= 0) continue;

                foreach (var pawn in pawnsData.CurrentMapPlayerPawns)
                {
                    var rowPawn = new float[variables.Count];
                    foreach (var workTypeKv in pawnsData.SortedPawnFitnessForEveryWork)
                    {
                        var wType = workTypeKv.Key;

                        if (!assignmentOffsets.TryGetValue((wType, pawn), out var offset)) continue;

                        rowPawn[offset + workTableIndex] = 1.0f;
                    }

                    model.AddConstraint(rowPawn, 0, maxJobs);
                }
            }

            alglib.minlpcreate(variables.Count, out var state);
            alglib.minlpsetcost(state, variables.ToArray().ToDouble());
            alglib.minlpsetbc(state, bndl.ToArray().ToDouble(), bndu.ToArray().ToDouble());

            foreach (var cRow in model.Constraints) alglib.minlpaddlc2dense(state, cRow.Coeff.ToDouble(), cRow.LowerBound, cRow.UpperBound);

            alglib.minlpsetscale(state, Enumerable.Repeat(1.0, variables.Count).ToArray());
            alglib.minlpsetalgoipm(state);
            alglib.minlpoptimize(state);

            alglib.minlpresults(state, out var solution, out var rep);

            if (rep.terminationtype < 1)
            {
                _logger.Warn(
                    $"{nameof(AssignPrioritiesByOptimization)} failed; "
                    + $"LP solver termination type: {rep.terminationtype}; abandoning assignment..."
                );
                onSolverFailed?.Invoke();
                return;
            }

            var sparseModel = model.CreateLinearModelSparse();
            var algorithm = new GeneticAlgorithm(
                _logger,
                _worldInfoRetriever,
                x => EvaluateSolution(x, sparseModel),
                solution,
                workTableEntries.Length,
                variables.Count / (workTableEntries.Length + 1),
                populationSize: _worldInfoRetriever.OptimizationPopulationSize(),
                secondsTimeout: _worldInfoRetriever.OptimizationFeasibleSolutionTimeoutSeconds(),
                secondsImproveSolution: _worldInfoRetriever.OptimizationImprovementSeconds(),
                crossoverRate: _worldInfoRetriever.OptimizationCrossoverRate(),
                mutationRate: _worldInfoRetriever.OptimizationMutationRate(),
                infeasiblePenalty: 1000000.0f
            );

            if (!algorithm.Run(out solution) || solution == null)
            {
                _logger.Warn(
                    $"{nameof(AssignPrioritiesByOptimization)} failed; "
                    + "random search failed to find a solution which satisfies constraints; abandoning assignment..."
                );
                onSolverFailed?.Invoke();
                return;
            }

            AssignPrioritiesFromSolution(workTableEntries, assignmentOffsets, solution.ToFloat(), pawnsData);
        }

        private void AssignPrioritiesFromSolution(WorkTableEntry[] workTableEntries,
            Dictionary<(IWorkTypeWrapper, IPawnWrapper), int> assignmentOffsets, float[] solution, PawnsData pawnsData)
        {
            var pawnWorkTypeVariables = new float[workTableEntries.Length + 1];

            foreach (var workTypeKv in pawnsData.SortedPawnFitnessForEveryWork)
            {
                var pawns = workTypeKv.Value;
                var workType = workTypeKv.Key;
                foreach (var pawnFitness in pawns)
                {
                    var ind = assignmentOffsets[(workType, pawnFitness.Pawn)];

                    for (var i = 0; i < pawnWorkTypeVariables.Length; i++)
                        pawnWorkTypeVariables[i] = solution[ind + i];

                    var chosenPriorityIndex = pawnWorkTypeVariables.ArgMax();

                    var priorityV = chosenPriorityIndex == workTableEntries.Length ? 0 : workTableEntries[chosenPriorityIndex].Priority.v;

                    if (_worldInfoRetriever.DebugLogs())
                    {
                        _logger.Info(
                            $"pawn {pawnFitness.Pawn.NameFullColored}; work type {workType.LabelShort}; "
                            + $"fitness {pawnFitness.Fitness}; chosen priority {priorityV}"
                        );
                    }

                    try
                    {
                        pawnFitness.Pawn.WorkSettingsSetPriority(workType, priorityV);
                    }
                    catch (Exception e)
                    {
                        _logger.Warn(
                            $"Failed to assign pawn {pawnFitness.Pawn.NameFullColored} to work type {workType.LabelShort} with priority {priorityV}: {e}"
                        );
                    }
                }
            }
        }

        private static bool CanPawnBeAssigned(PawnFitnessData pawnFitnessData, PawnsData pawnsData)
        {
            return (!pawnFitnessData.IsOpposed || pawnsData.IgnoreOppositionToWork)
                   && (pawnFitnessData.SkillLevel >= pawnsData.MinimumSkillLevel || !pawnFitnessData.IsSkilledWorkType);
        }

        private void AssignJobs(PawnsData pawnsData, IDictionary<IPawnWrapper, Dictionary<IWorkTypeWrapper, Priority>> pawnJobs,
            IEnumerable<IWorkTypeWrapper> workTypes, Func<IWorkTypeWrapper, List<PawnFitnessData>> fitnessGetter,
            List<(Priority priority, JobCount maxJobs, double percent)> priorityPercentCached, double? minimumSkillLevel = null)
        {
            foreach (var work in workTypes)
            {
                FillListPriorityPercents(pawnsData, work, priorityPercentCached);

                var pawns = fitnessGetter(work);
#if DEBUG
                _logger.Info($"worktype {work.DefName}");
#endif

#if DEBUG
                foreach (var (pawn, fitness, skillLevel, isOpposed, _) in pawns)
                    _logger.Info($"pawn {pawn.NameFullColored}; fitness {fitness}; skill {skillLevel}; isOpposed {isOpposed}");
#endif

                foreach (var (priority, maxJobs, jobsToSet) in priorityPercentCached.Distinct(x => x.priority)
                             .Select(a => a.percent)
                             .IterPercents(pawns.Count)
                             .GroupBy(v => v.percentIndex)
                             .Select(g => (priorityPercentCached[g.Key].priority, priorityPercentCached[g.Key].maxJobs, g.Count()))
                             .OrderBy(x => x.priority.v))
                {
                    var jobsSet = 0;
                    // iterate over all the pawns for this job with current priority
                    foreach (var (pawn, fitness, skillLevel, isOpposed, _) in pawns)
                    {
                        if (jobsSet >= jobsToSet)
                            break;

                        var jobsPawnHasOnThisPriority = pawnJobs[pawn].Count(kv => kv.Value.v == priority.v);
#if DEBUG
                        _logger.Info(
                            $"pawn {pawn.NameFullColored}; fitness {fitness}; jobsSet {jobsSet}; "
                            + $"jobsToSet {jobsToSet}; priority {priority.v}; "
                            + $"jobsPawnHasOnThisPriority {jobsPawnHasOnThisPriority}; maxJobs {maxJobs.v}"
                        );
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
                foreach (var (pawn, _, _, _, _) in pawns)
                {
                    // if this job was already set, skip
                    if (pawnJobs[pawn].ContainsKey(work))
                        continue;

                    pawn.WorkSettingsSetPriority(work, 0);
                }
            }
        }

        private static void FillListPriorityPercents(PawnsData pawnsData, IWorkTypeWrapper work,
            List<(Priority, JobCount, double)> priorities)
        {
            priorities.Clear();
            priorities.AddRange(
                pawnsData.WorkTables.Select(
                        tup => (priority: tup.Priority, jobCount: tup.JobCount,
                            pawnsData.PercentValue(tup.WorkTypes[work], work, tup.Priority))
                    )
                    .Distinct(t => t.priority)
                    .Where(t => t.priority.v > 0)
            );
            priorities.Sort((x, y) => x.Item1.v.CompareTo(y.Item1.v));
        }

        private (bool IsFeasible, double Objective) EvaluateSolution(double[] x, LinearModelSparse model)
        {
            // 1) Check variable bounds
            for (var i = 0; i < x.Length; i++)
                if (x[i] < model.LowerBounds[i] || x[i] > model.UpperBounds[i])
                    return (false, 0.0f); // Out-of-bounds => not feasible

            // 2) Check each constraint
            alglib.sparsemv(model.constraintCoeff, x, ref _matrixResultCached);
            for (var i = 0; i < model.constraintLowerBound.Length; i++)
            {
                var sum = _matrixResultCached[i];
                if (sum < model.constraintLowerBound[i] || sum > model.constraintUpperBound[i])
                    // Violates constraint
                    return (false, 0.0f);
            }

            // 3) Compute objective = dot(Cost, x).
            var objective = 0.0;
            for (var i = 0; i < x.Length; i++)
                objective += model.Cost[i] * x[i];

            return (true, objective);
        }

        private void SaveTablesAndPawnsIfDebug()
        {
            if (!_worldInfoRetriever.DebugSaveTablesAndPawns()) return;

            File.WriteAllBytes(
                "PrioritiesSmarterWorkTables.xml",
                new ArraySimpleData<WorkTablesSimpleData>(
                    _pawnsData.WorkTables.Select(
                            x => new WorkTablesSimpleData
                            {
                                priority = x.Priority,
                                jobCount = x.JobCount,
                                workTypes = x.WorkTypes.Select(
                                        y => new WorkTypesSimpleData { key = new WorkTypeSimpleData(y.Key), value = y.Value }
                                    )
                                    .ToList(),
                            }
                        )
                        .ToList()
                ).GetBytesXml()
            );

            File.WriteAllBytes(
                "PrioritiesSmarterWorkTypes.xml",
                new ArraySimpleData<WorkTypeSimpleData>(_pawnsData.WorkTypes.Select(y => new WorkTypeSimpleData(y)).ToList()).GetBytesXml()
            );
            File.WriteAllBytes(
                "PrioritiesSmarterAllPlayerPawns.xml",
                new ArraySimpleData<PawnSimpleData>(
                    _pawnsData.CurrentMapPlayerPawns.Select(
                            y => new PawnSimpleData(y)
                            {
                                pawnWorkTypeData = _pawnsData.WorkTypes.Select(x => new PawnWorkTypeData(y, x)).ToList(),
                            }
                        )
                        .ToList()
                ).GetBytesXml()
            );
        }
    }
}
