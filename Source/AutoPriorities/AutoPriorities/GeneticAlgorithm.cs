using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutoPriorities.APLogger;
using AutoPriorities.Utils.Extensions;
using AutoPriorities.WorldInfoRetriever;
using Verse;

namespace AutoPriorities
{
    public class GeneticAlgorithm
    {
        private readonly float _crossoverRate;
        private readonly int[] _freeGeneIndices;
        private readonly float _infeasiblePenalty; // penalty added if solution is infeasible
        private readonly ILogger _logger;
        private readonly LinearModel _model;
        private readonly float _mutationRate;

        // Number of priorities (K) for each (WorkType, Pawn), plus "0" = not assigned => total K+1
        private readonly int _numPriorities;

        // GA parameters (tweak them as you like)
        private readonly int _populationSize;

        private readonly Random _random = new(42);
        private readonly float _secondsImproveSolution;
        private readonly float _secondsTimeout;
        private readonly Chromosome _startingChromosome;
        private readonly IWorldInfoRetriever _worldInfoRetriever;

        public GeneticAlgorithm(ILogger logger, IWorldInfoRetriever worldInfoRetriever, double[] startingSolution, int numPriorities,
            LinearModel model, int[] freeVariableIndices, float secondsTimeout, float secondsImproveSolution, int populationSize = 30,
            float crossoverRate = 0.7f, float mutationRate = 0.1f, float infeasiblePenalty = 1_000_000.0f)
        {
            _logger = logger;
            _worldInfoRetriever = worldInfoRetriever;

            _numPriorities = numPriorities; // K
            _model = model;
            _secondsTimeout = secondsTimeout;
            _secondsImproveSolution = secondsImproveSolution;
            _populationSize = populationSize;
            _crossoverRate = crossoverRate;
            _mutationRate = mutationRate;
            _infeasiblePenalty = infeasiblePenalty;
            _startingChromosome = StartingSolutionToChromosome(startingSolution);
            _freeGeneIndices = FreeGenes(freeVariableIndices);
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
            var bestFitness = double.MaxValue; // because we are minimizing
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

        private List<Chromosome> RunGeneration(List<Chromosome> population, ref double bestFitness, ref Chromosome? bestSolution,
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

            // Fill the rest of the population
            while (newPopulation.Count < _populationSize)
            {
                // 4) Selection
                var parent1 = TournamentSelection(scoredPopulation);
                var parent2 = TournamentSelection(scoredPopulation);

                // 5) Crossover
                var (child1, child2) = UniformCrossover(parent1, parent2, _crossoverRate);

                // 6) Mutation
                Mutate(ref child1, _mutationRate);
                Mutate(ref child2, _mutationRate);

                newPopulation.Add(child1);
                if (newPopulation.Count < _populationSize)
                    newPopulation.Add(child2);
            }

            population = newPopulation;
            return population;
        }

        private Chromosome StartingSolutionToChromosome(double[] startingSolution)
        {
            // Each chunk is size (_numPriorities + 1).
            // We have _numWorkTypePawnPairs such chunks in _startingSolution.
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

                // bestIndex is the chosen priority for pair i
                chromosome.chrom[i] = bestIndex;
            }

            return chromosome;
        }

        private int[] FreeGenes(int[] freeVariableIndices)
        {
            var freeGenes = new int[freeVariableIndices.Length];

            for (var i = 0; i < freeVariableIndices.Length; i++) freeGenes[i] = freeVariableIndices[i] / (_numPriorities + 1);

            return freeGenes;
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

        /// <summary>
        ///     Compute the "fitness" of an individual (chromosome).
        ///     We treat _evaluateFunc as returning (feasible, cost).
        ///     Since the solver is set up for "minimize cost," we interpret "fitness = cost + penalty if infeasible."
        ///     Smaller fitness is better.
        /// </summary>
        private double Fitness(Chromosome chromosome, out bool isFeasible)
        {
            var x = ChromosomeToDoubleArray(chromosome);
            var (feasible, cost) = EvaluateSolution(x);

            isFeasible = feasible;

            return isFeasible ? cost : cost + _infeasiblePenalty; // lower is better
        }

        /// <summary>
        ///     Mutate a chromosome by picking exactly 1 random gene
        ///     and setting it to a random priority in [0.._numPriorities].
        ///     If you want multiple mutations per chromosome or a different mutation approach,
        ///     you can tweak it here.
        /// </summary>
        private void Mutate(ref Chromosome chrom, double mutationRate)
        {
            if (_random.NextDouble() < mutationRate)
            {
                // pick a random gene
                var idx = _random.Next(0, _freeGeneIndices.Length);
                var freeGeneIndex = _freeGeneIndices[idx];

                // set it to a new random priority
                chrom.chrom[freeGeneIndex] = _random.Next(0, _numPriorities + 1);
            }
        }

        /// <summary>
        ///     Uniform crossover: for each gene, pick from parent1 or parent2 with 50% chance.
        ///     If random > crossoverRate, we just clone parents.
        /// </summary>
        private (Chromosome, Chromosome) UniformCrossover(Chromosome parent1, Chromosome parent2, double rate)
        {
            if (_random.NextDouble() > rate)
            {
                // No crossover => return clones
                return (parent1, parent2);
            }

            var length = parent1.chrom.Length;
            var child1 = new Chromosome { chrom = new int[length] };
            var child2 = new Chromosome { chrom = new int[length] };

            foreach (var freeGeneIndex in _freeGeneIndices)
            {
                if (_random.NextDouble() < 0.5)
                {
                    child1.chrom[freeGeneIndex] = parent1.chrom[freeGeneIndex];
                    child2.chrom[freeGeneIndex] = parent2.chrom[freeGeneIndex];
                }
                else
                {
                    child1.chrom[freeGeneIndex] = parent2.chrom[freeGeneIndex];
                    child2.chrom[freeGeneIndex] = parent1.chrom[freeGeneIndex];
                }
            }

            return (child1, child2);
        }

        /// <summary>
        ///     Simple tournament selection.
        ///     We pick N random solutions, pick the best (lowest fitness).
        /// </summary>
        private Chromosome TournamentSelection(List<(Chromosome chrom, double fitness, bool isFeasible)> scoredPopulation,
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

        private (bool IsFeasible, float Objective) EvaluateSolution(float[] x)
        {
            // 1) Check variable bounds
            for (var i = 0; i < x.Length; i++)
                if (x[i] < _model.LowerBounds[i] || x[i] > _model.UpperBounds[i])
                    return (false, 0.0f); // Out-of-bounds => not feasible

            // 2) Check each constraint
            foreach (var cRow in _model.Constraints)
            {
                var sum = 0.0;
                for (var i = 0; i < x.Length; i++) sum += cRow.Coeff[i] * x[i];

                if (sum < cRow.LowerBound || sum > cRow.UpperBound)
                    // Violates constraint
                    return (false, 0.0f);
            }

            // 3) Compute objective = dot(Cost, x).
            var objective = 0.0f;
            for (var i = 0; i < x.Length; i++) objective += _model.Cost[i] * x[i];

            return (true, objective);
        }

        private struct Chromosome
        {
            public int[] chrom;
        }
    }
}
