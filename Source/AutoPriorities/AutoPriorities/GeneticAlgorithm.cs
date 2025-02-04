using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutoPriorities.Utils.Extensions;
using AutoPriorities.WorldInfoRetriever;
using Verse;
using ILogger = AutoPriorities.APLogger.ILogger;
using Random = System.Random;

namespace AutoPriorities
{
    public class GeneticAlgorithm
    {
        private readonly float _crossoverRate;
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
            float crossoverRate = 0.7f, float mutationRate = 0.1f, float infeasiblePenalty = 1_000_000.0f)
        {
            _logger = logger;
            _worldInfoRetriever = worldInfoRetriever;

            _numPriorities = numPriorities; // K
            _model = new LinearModelOpt(model, sumPrioritiesCount);
            _secondsTimeout = secondsTimeout;
            _secondsImproveSolution = secondsImproveSolution;
            _populationSize = populationSize;
            _crossoverRate = crossoverRate;
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
            var elitesCount = (int)(0.2 * _populationSize);
            for (var e = 0; e < elitesCount; e++) newPopulation.Add(scoredPopulation[e].chrom);

            // add retain some infeasible solutions
            var firstInfeasibleIndex = scoredPopulation.FindIndex(x => !x.isFeasible);
            for (var e = firstInfeasibleIndex; e < elitesCount + firstInfeasibleIndex && e < scoredPopulation.Count; e++)
                newPopulation.Add(scoredPopulation[e].chrom);

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
            // random number of mutations
            for (var idx = 0; idx < _random.Next((int)(_freeGeneIndices.Length * mutationRate)); idx++)
            {
                // pick a random gene index among the "free" ones
                // var idx = _random.Next(0, _freeGeneIndices.Length);
                var freeGeneIndex = _freeGeneIndices[idx];

                // oldPriority -> newPriority
                var oldPriority = chrom.chrom[freeGeneIndex];
                var newPriority = _random.Next(0, _numPriorities + 1);
                if (newPriority == oldPriority)
                    return; // no actual change

                // === INCREMENTAL UPDATE: remove old, add new ===
                var chunkSize = _numPriorities + 1;

                var oldVarIndex = freeGeneIndex * chunkSize + oldPriority;
                var newVarIndex = freeGeneIndex * chunkSize + newPriority;

                // 1) Subtract old priority's cost, add new
                chrom.objective -= _model.Cost[oldVarIndex];
                chrom.objective += _model.Cost[newVarIndex];

                // 2) For each constraint, subtract old coefficient, add new
                for (var cIndex = 0; cIndex < chrom.constraintSums.Length; cIndex++)
                {
                    chrom.constraintSums[cIndex] -= _model.constraintCoeff[cIndex, oldVarIndex];
                    chrom.constraintSums[cIndex] += _model.constraintCoeff[cIndex, newVarIndex];
                }

                // 3) Update the gene in the chromosome
                chrom.chrom[freeGeneIndex] = newPriority;

                // 4) Re-check feasibility and update fitness
                chrom.isFeasible = CheckFeasibleAndBounds(chrom);
                chrom.fitness = chrom.isFeasible ? chrom.objective : _infeasiblePenalty;
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
        ///     Fill Chromosome.constraintSums and Chromosome.objective from scratch.
        ///     Then check feasibility and set Chromosome.fitness accordingly.
        /// </summary>
        private void InitializeChromosomeFields(ref Chromosome c)
        {
            // Prepare to accumulate
            var constraintCount = _model.constraintCoeff.GetLength(0);
            c.constraintSums = new float[constraintCount];
            c.objective = 0.0f;

            // We'll build the partial sums by iterating over each gene.
            // For gene i, the "expanded" variable that is 1 is offset + chosenPriority.
            var chunkSize = _numPriorities + 1;

            for (var i = 0; i < c.chrom.Length; i++)
            {
                var chosenPriority = c.chrom[i];
                var offset = i * chunkSize + chosenPriority;

                // Add to objective
                c.objective += _model.Cost[offset];

                // Add to each constraint's sum
                for (var constrIndex = 0; constrIndex < constraintCount; constrIndex++)
                    c.constraintSums[constrIndex] += _model.constraintCoeff[constrIndex, offset];
            }

            // Now check feasibility and set fitness
            c.isFeasible = CheckFeasibleAndBounds(c);
            c.fitness = c.isFeasible ? c.objective : c.objective + _infeasiblePenalty;
        }

        /// <summary>
        ///     Return true if all constraints and variable bounds are satisfied.
        ///     We already have partial sums for constraints,
        ///     but we still check them individually for feasibility.
        /// </summary>
        private bool CheckFeasibleAndBounds(Chromosome c)
        {
            // 1) Check constraint bounds using partial sums
            for (var i = 0; i < _model.constraintCoeff.GetLength(0); i++)
            {
                var sum = c.constraintSums[i];
                var lowerBound = _model.constraintLowerBound[i];
                var upperBound = _model.constraintUpperBound[i];
                if (sum < lowerBound || sum > upperBound) return false;
            }

            return true;
        }

        private struct Chromosome
        {
            public int[] chrom; // The gene array: which priority index is used for each "slot"
            public float[] constraintSums; // One sum per constraint
            public float objective; // dot(Cost, x)
            public float fitness; // objective (+ penalty if infeasible)
            public bool isFeasible; // True if all constraints/bounds satisfied

            public Chromosome Clone()
            {
                // Shallow copy of the struct
                var clone = this;

                // Deep copy of the int[] array using CopyTo
                clone.chrom = new int[chrom.Length];
                chrom.CopyTo(clone.chrom, 0);

                // Deep copy of the float[] array using CopyTo
                clone.constraintSums = new float[constraintSums.Length];
                constraintSums.CopyTo(clone.constraintSums, 0);

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
