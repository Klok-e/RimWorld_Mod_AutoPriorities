using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutoPriorities.Utils.Extensions;
using AutoPriorities.WorldInfoRetriever;
using UnityEngine;
using Verse;
using ILogger = AutoPriorities.APLogger.ILogger;
using Random = System.Random;

namespace AutoPriorities
{
    public class GeneticAlgorithm
    {
        private readonly int[] _freeGeneIndices;
        private readonly float _infeasiblePenalty; // penalty added if solution is infeasible
        private readonly float _jobsPerPawnWeight;
        private readonly ILogger _logger;
        private readonly LinearModelOpt _model;
        private readonly float _mutationRate;

        // Number of priorities (K) for each (WorkType, Pawn), plus "0" = not assigned => total K+1
        private readonly int _numPriorities;

        // GA parameters (tweak them as you like)
        private readonly int _populationSize;

        private readonly Random _random = new();
        private readonly float _secondsImproveSolution;
        private readonly float _secondsTimeout;
        private readonly Chromosome _startingChromosome;
        private readonly IWorldInfoRetriever _worldInfoRetriever;

        public GeneticAlgorithm(ILogger logger, IWorldInfoRetriever worldInfoRetriever, double[] startingSolution, int numPriorities,
            LinearModel model, float secondsTimeout, float secondsImproveSolution, int populationSize = 30, float mutationRate = 0.1f,
            float infeasiblePenalty = 1_000_000.0f, float jobsPerPawnWeight = 0.01f)
        {
            _logger = logger;
            _worldInfoRetriever = worldInfoRetriever;

            _numPriorities = numPriorities; // K
            _model = new LinearModelOpt(model);
            _secondsTimeout = secondsTimeout;
            _secondsImproveSolution = secondsImproveSolution;
            _populationSize = populationSize;
            _mutationRate = mutationRate;
            _infeasiblePenalty = infeasiblePenalty;
            _jobsPerPawnWeight = jobsPerPawnWeight;
            (_startingChromosome, _freeGeneIndices) = StartingSolutionToChromosome(startingSolution);
        }

        /// <summary>
        ///     Runs the genetic algorithm and returns the best solution found
        ///     as a "double[]" that you can pass to EvaluateSolution again or apply in the game.
        /// </summary>
        public bool Run(out float[]? solution)
        {
            // 1) Generate initial population of chromosomes
            var population = new List<Chromosome>(_populationSize);
            for (var i = 0; i < _populationSize; i++) population.Add(_startingChromosome);

            // Keep track of overall best solution
            Chromosome? bestSolution = null;
            var bestFitness = float.MaxValue; // because we are minimizing
            var bestIsFeasible = false;

            var generations = 0;

            var timer = new Stopwatch();
            timer.Start();

            while (!bestIsFeasible && timer.Elapsed.TotalSeconds < _secondsTimeout)
            {
                population = RunGeneration(population, ref bestFitness, ref bestSolution, ref bestIsFeasible);

                generations++;
            }

            if (_worldInfoRetriever.DebugLogs())
                _logger.Info($"Random search run to find feasible solution for seconds: {timer.Elapsed.TotalSeconds}");

            timer.Restart();
            while (timer.Elapsed.TotalSeconds < _secondsImproveSolution)
            {
                population = RunGeneration(population, ref bestFitness, ref bestSolution, ref bestIsFeasible);

                generations++;
            }

            if (_worldInfoRetriever.DebugLogs())
                _logger.Info(
                    $"Random search run to improve on solution for seconds: {timer.Elapsed.TotalSeconds}. Generations: {generations}"
                );

            if (bestSolution is { } value)
                solution = ChromosomeToDoubleArray(value);
            else
                solution = null;

            return bestIsFeasible;
        }

        private List<Chromosome> RunGeneration(List<Chromosome> population, ref float bestFitness, ref Chromosome? bestSolution,
            ref bool bestIsFeasible)
        {
            // Evaluate all individuals and sort them by fitness
            var scoredPopulation = population.Select(chrom => (chrom, fitness: Fitness(chrom, out var isFeasible), isFeasible)).ToList();
            scoredPopulation.Shuffle(_random);
            scoredPopulation.SortBy(t => t.fitness);

            // Update best solution
            if (scoredPopulation[0].fitness < bestFitness)
            {
                bestFitness = scoredPopulation[0].fitness;
                bestSolution = scoredPopulation[0].chrom;
                bestIsFeasible = scoredPopulation[0].isFeasible;
            }

            // always keep starting chromosome
            var newPopulation = new List<Chromosome>(_populationSize) { _startingChromosome };

            // Keep some elites (the top few) to carry over
            var elitesCount = (int)(0.1 * _populationSize);
            for (var e = 0; e < elitesCount; e++) newPopulation.Add(scoredPopulation[e].chrom);

            // add retain some infeasible solutions
            var infeasibleElitesCount = (int)(0.2 * _populationSize);
            var firstInfeasibleIndex = scoredPopulation.FindIndex(x => !x.isFeasible);
            if (firstInfeasibleIndex >= 0)
            {
                for (var e = firstInfeasibleIndex; e < infeasibleElitesCount + firstInfeasibleIndex && e < scoredPopulation.Count; e++)
                    newPopulation.Add(scoredPopulation[e].chrom);
            }

            // Fill the rest of the population
            while (newPopulation.Count < _populationSize)
            {
                var parent = TournamentSelection(scoredPopulation);

                var child = parent.Clone();

                Mutate(ref child, _mutationRate);

                newPopulation.Add(child);
            }

            population = newPopulation;
            return population;
        }

        private (Chromosome, int[] freeGeneIndices) StartingSolutionToChromosome(double[] startingSolution)
        {
            var freeVariableIndices = new List<int>();

            var chunkSize = _numPriorities + 1;
            var genes = startingSolution.Length / chunkSize;
            var chromosome = new Chromosome { chrom = new int[genes] };

            for (var i = 0; i < genes; i++)
            {
                var offset = i * chunkSize;

                // Find which index in [offset .. offset+chunkSize-1] has the largest value
                var bestIndex = 0;
                var bestValue = double.NegativeInfinity;

                for (var j = 0; j < chunkSize; j++)
                {
                    var val = startingSolution[offset + j];
                    if (val > bestValue)
                    {
                        bestValue = val;
                        bestIndex = j;
                    }
                }

                if (bestValue is > 0.001 and < 0.999)
                    freeVariableIndices.Add(i);

                // bestIndex is the chosen priority for pair i
                chromosome.chrom[i] = bestIndex;
            }

            InitializeChromosomeFields(ref chromosome);

            return (chromosome, freeVariableIndices.ToArray());
        }

        /// <summary>
        ///     Convert a chromosome (int[] of length = #_pairs, each in [0.._numPriorities])
        ///     to a double[] for EvaluateSolution.
        /// </summary>
        private float[] ChromosomeToDoubleArray(Chromosome chromosome)
        {
            // We have (#pairs) chunks, each chunk size = (_numPriorities + 1).
            // Initialize a big zero array for all bits.
            var x = new float[chromosome.chrom.Length * (_numPriorities + 1)];

            for (var i = 0; i < chromosome.chrom.Length; i++)
            {
                var chosenPriority = chromosome.chrom[i];

                var offset = i * (_numPriorities + 1);
                x[offset + chosenPriority] = 1.0f;
            }

            return x;
        }

        private float Fitness(Chromosome chromosome, out bool isFeasible)
        {
            // We rely on the incremental (or from-scratch) updates
            // that already filled in .fitness and .isFeasible
            // so there's no big re-evaluation here.
            isFeasible = chromosome.isFeasible;
            return chromosome.fitness;
        }

        private void Mutate(ref Chromosome chrom, double mutationRate)
        {
            // random number of genes to mutate
            var numMutations = _random.Next((int)(_freeGeneIndices.Length * mutationRate));
            if (numMutations == 0) return; // nothing changes, so no recalc needed

            _freeGeneIndices.Shuffle(_random);

            // Apply incremental updates for each mutated gene
            var freeGeneArrayIndex = 0;
            for (var m = 0; m < numMutations; m++)
            {
                var freeGeneIndex = _freeGeneIndices[freeGeneArrayIndex++];

                var oldPriority = chrom.chrom[freeGeneIndex];
                var newPriority = _random.Next(0, _numPriorities + 1);
                if (newPriority == oldPriority)
                    continue; // no actual change

                // === INCREMENTAL UPDATE ===
                var chunkSize = _numPriorities + 1;
                var oldVarIndex = freeGeneIndex * chunkSize + oldPriority;
                var newVarIndex = freeGeneIndex * chunkSize + newPriority;

                // 1) Objective
                chrom.objective -= _model.Cost[oldVarIndex];
                chrom.objective += _model.Cost[newVarIndex];

                // 2) For each constraint
                for (var cIndex = 0; cIndex < chrom.constraintSums.Length; cIndex++)
                {
                    var oldSum = chrom.constraintSums[cIndex];
                    var oldViolation = chrom.constraintViolations[cIndex];

                    // Update partial sum
                    var updatedSum = oldSum - _model.constraintCoeff[cIndex, oldVarIndex] + _model.constraintCoeff[cIndex, newVarIndex];
                    chrom.constraintSums[cIndex] = updatedSum;

                    // Compute new violation for this constraint
                    var lb = _model.constraintLowerBound[cIndex];
                    var ub = _model.constraintUpperBound[cIndex];
                    var newViolation = 0f;

                    if (updatedSum < lb)
                        newViolation = lb - updatedSum;
                    else if (updatedSum > ub)
                        newViolation = updatedSum - ub;

                    // 3) Update totalViolation
                    var diff = newViolation - oldViolation;
                    chrom.totalViolation += diff;
                    chrom.constraintViolations[cIndex] = newViolation;
                }

                // Finally, update the gene in the chromosome
                chrom.chrom[freeGeneIndex] = newPriority;
            }

            RecomputeSumOfMaxPriorities(ref chrom);

            // 4) Re-check feasibility and update fitness
            chrom.isFeasible = Mathf.Approximately(chrom.totalViolation, 0f);
            if (chrom.isFeasible)
                chrom.fitness = chrom.objective + chrom.sumOfMaxPriorities * _jobsPerPawnWeight;
            else
                chrom.fitness = chrom.totalViolation + _infeasiblePenalty;
        }

        /// <summary>
        ///     Simple tournament selection.
        ///     We pick N random solutions, pick the best (lowest fitness).
        /// </summary>
        private Chromosome TournamentSelection(List<(Chromosome chrom, float fitness, bool isFeasible)> scoredPopulation,
            int tournamentSize = 3)
        {
            var bestIndex = -1;
            var bestFitness = double.MaxValue;

            for (var i = 0; i < tournamentSize; i++)
            {
                var r = _random.Next(scoredPopulation.Count);
                var candidate = scoredPopulation[r];
                if (candidate.fitness < bestFitness)
                {
                    bestIndex = r;
                    bestFitness = candidate.fitness;
                }
            }

            // Return the chromosome of the best in the random subset
            return scoredPopulation[bestIndex].chrom;
        }

        /// <summary>
        /// Fill Chromosome.constraintSums and Chromosome.objective from scratch,
        /// then compute constraintViolations & totalViolation,
        /// then set isFeasible and fitness accordingly.
        /// </summary>
        private void InitializeChromosomeFields(ref Chromosome c)
        {
            var constraintCount = _model.constraintCoeff.GetLength(0);
            c.constraintSums = new float[constraintCount];
            c.constraintViolations = new float[constraintCount];
            c.objective = 0.0f;
            c.totalViolation = 0.0f;

            // Build partial sums (one nonzero variable per gene)
            var chunkSize = _numPriorities + 1;
            for (var i = 0; i < c.chrom.Length; i++)
            {
                var chosenPriority = c.chrom[i];
                var offset = i * chunkSize + chosenPriority;

                // Objective
                c.objective += _model.Cost[offset];

                // Each constraint's sum
                for (var constrIndex = 0; constrIndex < constraintCount; constrIndex++)
                {
                    c.constraintSums[constrIndex] += _model.constraintCoeff[constrIndex, offset];
                }
            }

            // Compute violations for each constraint
            for (var i = 0; i < constraintCount; i++)
            {
                var sum = c.constraintSums[i];
                var lb = _model.constraintLowerBound[i];
                var ub = _model.constraintUpperBound[i];

                var violation = 0f;
                if (sum < lb)
                    violation = lb - sum;
                else if (sum > ub)
                    violation = sum - ub;

                c.constraintViolations[i] = violation;
                c.totalViolation += violation;
            }

            RecomputeSumOfMaxPriorities(ref c);

            // Now set feasibility & fitness
            c.isFeasible = c.totalViolation == 0.0f;
            if (c.isFeasible)
                c.fitness = c.objective + c.sumOfMaxPriorities * _jobsPerPawnWeight;
            else
                c.fitness = c.totalViolation + _infeasiblePenalty;
        }

        private void RecomputeSumOfMaxPriorities(ref Chromosome chrom)
        {
            chrom.sumOfMaxPriorities = 0f;

            // For each priority "group"
            foreach (var jobPriorityRange in _model.JobsConstraintPriorityRanges)
            {
                var maxVal = float.NegativeInfinity;
                // Find the maximum partial sum in that group
                for (var i = jobPriorityRange.indexStart; i <= jobPriorityRange.indexEnd; i++)
                {
                    var val = chrom.constraintSums[i];
                    if (val > maxVal)
                        maxVal = val;
                }

                // Accumulate into sumOfMaxPriorities
                chrom.sumOfMaxPriorities += maxVal;
            }
        }

        private struct Chromosome
        {
            public int[] chrom; // The gene array: which priority index is used for each "slot"
            public float[] constraintSums; // One partial sum per constraint
            public float[] constraintViolations; // How far each constraint is from feasibility
            public float totalViolation; // Sum of all constraintViolations

            public float objective; // dot(Cost, x)
            public float sumOfMaxPriorities; // sum(max(jobPriorityConstraintSum))
            public float fitness; // If feasible: = objective; otherwise = objective + totalViolation + penalty
            public bool isFeasible; // True if totalViolation == 0

            public Chromosome Clone()
            {
                // Shallow copy of 'this'
                var clone = this;
                // Deep copy arrays
                clone.chrom = (int[])chrom.Clone();
                clone.constraintSums = (float[])constraintSums.Clone();
                clone.constraintViolations = (float[])constraintViolations.Clone();
                return clone;
            }
        }

        public class LinearModelOpt
        {
            public float[,] constraintCoeff;
            public float[] constraintLowerBound;
            public float[] constraintUpperBound;

            public LinearModelOpt(LinearModel model)
            {
                VariableCount = model.VariableCount;
                Cost = model.Cost;
                LowerBounds = model.LowerBounds;
                UpperBounds = model.UpperBounds;

                // 1) Figure out how many total constraints = "regular" + "max jobs"
                var totalConstraints = model.Constraints.Count + model.MaxJobsConstraints.Count;

                // 2) Allocate matrix + bound arrays
                constraintCoeff = new float[totalConstraints, VariableCount];
                constraintLowerBound = new float[totalConstraints];
                constraintUpperBound = new float[totalConstraints];

                // 3) First, copy "regular" constraints
                for (var row = 0; row < model.Constraints.Count; row++)
                {
                    var mc = model.Constraints[row];
                    for (var col = 0; col < VariableCount; col++)
                        constraintCoeff[row, col] = mc.Coeff[col];

                    constraintLowerBound[row] = mc.LowerBound;
                    constraintUpperBound[row] = mc.UpperBound;
                }

                // 4) Next, copy "max jobs" constraints, if any
                var offset = model.Constraints.Count;
                for (var i = 0; i < model.MaxJobsConstraints.Count; i++)
                {
                    var mc = model.MaxJobsConstraints[i];
                    var row = offset + i;
                    for (var col = 0; col < VariableCount; col++)
                        constraintCoeff[row, col] = mc.Coeff[col];

                    constraintLowerBound[row] = mc.LowerBound;
                    constraintUpperBound[row] = mc.UpperBound;
                }

                // 5) Build "priority ranges" only if MaxJobsConstraints is non-empty
                if (model.MaxJobsConstraints.Count <= 0) return;

                var maxJobConstraintRanges = new List<MaxJobCountIndices>();

                // We'll track the start of a "block" of constraints all sharing
                // the same mc.PriorityIndex.
                var groupStartIndex = offset; // where "max jobs" constraints begin
                var currentPriorityIndex = model.MaxJobsConstraints[0].PriorityIndex;

                // We'll iterate from the 2nd constraint onward
                for (var i = 1; i < model.MaxJobsConstraints.Count; i++)
                {
                    var mc = model.MaxJobsConstraints[i];
                    if (mc.PriorityIndex != currentPriorityIndex)
                    {
                        // We reached a new priority => close out the old group
                        maxJobConstraintRanges.Add(
                            new MaxJobCountIndices
                            {
                                indexStart = groupStartIndex,
                                // The group ends at row (offset + i - 1)
                                indexEnd = offset + i - 1,
                            }
                        );

                        // Start a new group
                        groupStartIndex = offset + i;
                        currentPriorityIndex = mc.PriorityIndex;
                    }
                }

                // Add the final group
                maxJobConstraintRanges.Add(
                    new MaxJobCountIndices { indexStart = groupStartIndex, indexEnd = offset + model.MaxJobsConstraints.Count - 1 }
                );

                JobsConstraintPriorityRanges = maxJobConstraintRanges.ToArray();
            }

            public MaxJobCountIndices[] JobsConstraintPriorityRanges { get; } = Array.Empty<MaxJobCountIndices>();

            public int VariableCount { get; }

            public float[] Cost { get; }
            public float[] LowerBounds { get; }
            public float[] UpperBounds { get; }

            // A struct marking the [start..end] rows for each group of constraints 
            // that share one PriorityIndex.
            public struct MaxJobCountIndices
            {
                public int indexStart;
                public int indexEnd;
            }
        }
    }
}
