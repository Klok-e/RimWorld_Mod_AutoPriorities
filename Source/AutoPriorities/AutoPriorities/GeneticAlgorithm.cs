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
            LinearModel model, int sumPrioritiesCount, float secondsTimeout, float secondsImproveSolution, int populationSize = 30,
            float mutationRate = 0.1f, float infeasiblePenalty = 1_000_000.0f)
        {
            _logger = logger;
            _worldInfoRetriever = worldInfoRetriever;

            _numPriorities = numPriorities; // K
            _model = new LinearModelOpt(model, sumPrioritiesCount);
            _secondsTimeout = secondsTimeout;
            _secondsImproveSolution = secondsImproveSolution;
            _populationSize = populationSize;
            _mutationRate = mutationRate;
            _infeasiblePenalty = infeasiblePenalty;
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
                _logger.Info($"Random search run to improve on solution for seconds: {timer.Elapsed.TotalSeconds}");

            Console.WriteLine($"Generations: {generations}");

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

            // Print or log some info if you want
            // Console.WriteLine($"Gen {gen}, BestFitness: {bestFitness}");

            // 3) Selection + Reproduction
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
                // 4) Selection
                var parent = TournamentSelection(scoredPopulation);

                // 5) Crossover
                var child = parent.Clone();

                // 6) Mutation
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

                if (bestValue is > 0.001 and < 0.999) freeVariableIndices.Add(i);

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

            for (var i = 0; i < numMutations; i++)
            {
                var m = _random.Next(0, _freeGeneIndices.Length);
                var freeGeneIndex = _freeGeneIndices[m];

                // oldPriority -> newPriority
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

                    // Compute new violation
                    var lb = _model.constraintLowerBound[cIndex];
                    var ub = _model.constraintUpperBound[cIndex];

                    var newViolation = 0f;
                    if (updatedSum < lb)
                        newViolation = lb - updatedSum;
                    else if (updatedSum > ub)
                        newViolation = updatedSum - ub;

                    // 3) Update total violation
                    var difference = newViolation - oldViolation;
                    chrom.totalViolation += difference;
                    chrom.constraintViolations[cIndex] = newViolation;
                }

                // 4) Re-check feasibility and update fitness
                chrom.isFeasible = Mathf.Approximately(chrom.totalViolation, 0f);
                if (chrom.isFeasible)
                    chrom.fitness = chrom.objective;
                else
                    chrom.fitness = chrom.totalViolation + _infeasiblePenalty;

                // 5) Finally, update the gene in the chromosome
                chrom.chrom[freeGeneIndex] = newPriority;
            }
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

            // Now set feasibility & fitness
            c.isFeasible = c.totalViolation == 0.0f;
            if (c.isFeasible)
                c.fitness = c.objective;
            else
                c.fitness = c.totalViolation + _infeasiblePenalty;
        }

        private struct Chromosome
        {
            public int[] chrom; // The gene array: which priority index is used for each "slot"
            public float[] constraintSums; // One partial sum per constraint
            public float[] constraintViolations; // How far each constraint is from feasibility
            public float totalViolation; // Sum of all constraintViolations

            public float objective; // dot(Cost, x)
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

            public LinearModelOpt(LinearModel model, int skipConstraintsCount)
            {
                VariableCount = model.VariableCount;
                Cost = model.Cost;
                LowerBounds = model.LowerBounds;
                UpperBounds = model.UpperBounds;

                var newConstraintsCount = model.Constraints.Count - skipConstraintsCount;
                constraintCoeff = new float[newConstraintsCount, VariableCount];
                constraintLowerBound = new float[newConstraintsCount];
                constraintUpperBound = new float[newConstraintsCount];

                for (var index = 0; index < newConstraintsCount; index++)
                {
                    var modelConstraint = model.Constraints[skipConstraintsCount + index];
                    for (var i = 0; i < modelConstraint.Coeff.Length; i++) constraintCoeff[index, i] = modelConstraint.Coeff[i];

                    constraintLowerBound[index] = modelConstraint.LowerBound;
                    constraintUpperBound[index] = modelConstraint.UpperBound;
                }

                var a = 1;
            }

            public int VariableCount { get; }

            public float[] Cost { get; }

            public float[] LowerBounds { get; private set; }
            public float[] UpperBounds { get; private set; }
        }
    }
}
