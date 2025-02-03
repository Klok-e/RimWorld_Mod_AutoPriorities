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
        private readonly Func<double[], (bool IsFeasible, double Obj)> _evaluateFunc;
        private readonly float _infeasiblePenalty; // penalty added if solution is infeasible
        private readonly ILogger _logger;
        private readonly float _mutationRate;

        // Number of priorities (K) for each (WorkType, Pawn), plus "0" = not assigned => total K+1
        private readonly int _numPriorities;
        private readonly int _numWorkTypePawnPairs;

        // GA parameters (tweak them as you like)
        private readonly int _populationSize;

        private readonly Random _random = new(42);
        private readonly float _secondsImproveSolution;
        private readonly float _secondsTimeout;
        private readonly int[] _startingChromosome;
        private readonly double[] _startingSolution;
        private readonly IWorldInfoRetriever _worldInfoRetriever;

        public GeneticAlgorithm(ILogger logger, IWorldInfoRetriever worldInfoRetriever,
            Func<double[], (bool IsFeasible, double Obj)> evaluateFunc, double[] startingSolution, int numPriorities, int numWorkTypePawnPairs,
            float secondsTimeout, float secondsImproveSolution, int populationSize = 30, float crossoverRate = 0.7f,
            float mutationRate = 0.1f, float infeasiblePenalty = 1_000_000.0f)
        {
            _logger = logger;
            _worldInfoRetriever = worldInfoRetriever;
            _evaluateFunc = evaluateFunc;
            _startingSolution = startingSolution;

            _numPriorities = numPriorities; // K
            _numWorkTypePawnPairs = numWorkTypePawnPairs;
            _secondsTimeout = secondsTimeout;
            _secondsImproveSolution = secondsImproveSolution;
            _populationSize = populationSize;
            _crossoverRate = crossoverRate;
            _mutationRate = mutationRate;
            _infeasiblePenalty = infeasiblePenalty;
            _startingChromosome = StartingSolutionToChromosome();
        }

        /// <summary>
        ///     Runs the genetic algorithm and returns the best solution found
        ///     as a "double[]" that you can pass to EvaluateSolution again or apply in the game.
        /// </summary>
        public bool Run(out double[]? solution)
        {
            // 1) Generate initial population of chromosomes
            var population = new List<int[]>(_populationSize);
            for (var i = 0; i < _populationSize; i++) population.Add(_startingChromosome);

            // Keep track of overall best solution
            double[]? bestSolution = null;
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

            solution = bestSolution;

            return bestIsFeasible;
        }

        private List<int[]> RunGeneration(List<int[]> population, ref double bestFitness, ref double[]? bestSolution, ref bool bestIsFeasible)
        {
            
            // Evaluate all individuals and sort them by fitness
            var popFitnesses = population.Select(chrom => (chrom, fitness: Fitness(chrom, out var isFeasible), isFeasible)).ToList();
            popFitnesses.Shuffle(_random);
            
            var scoredPopulation = popFitnesses
                .OrderBy(t => t.fitness)
                .ToList();

            // Update best solution
            if (scoredPopulation[0].fitness < bestFitness)
            {
                bestFitness = scoredPopulation[0].fitness;
                bestSolution = ChromosomeToDoubleArray(scoredPopulation[0].chrom);
                bestIsFeasible = scoredPopulation[0].isFeasible;
            }

            // Print or log some info if you want
            // Console.WriteLine($"Gen {gen}, BestFitness: {bestFitness}");

            // 3) Selection + Reproduction
            // always keep starting chromosome
            var newPopulation = new List<int[]>(_populationSize) { _startingChromosome };

            // Keep some elites (the top few) to carry over
            var elitesCount = (int)(0.3 * _populationSize);
            for (var e = 0; e < elitesCount; e++) newPopulation.Add(scoredPopulation[e].chrom);

            // Fill the rest of the population
            while (newPopulation.Count < _populationSize)
            {
                // 4) Selection
                var parent1 = TournamentSelection(scoredPopulation);
                var parent2 = TournamentSelection(scoredPopulation);

                // 5) Crossover
                int[] child1, child2;
                (child1, child2) = UniformCrossover(parent1, parent2, _crossoverRate);

                // 6) Mutation
                Mutate(child1, _mutationRate);
                Mutate(child2, _mutationRate);

                newPopulation.Add(child1);
                if (newPopulation.Count < _populationSize)
                    newPopulation.Add(child2);
            }

            population = newPopulation;
            return population;
        }

        private int[] StartingSolutionToChromosome()
        {
            // Each chunk is size (_numPriorities + 1).
            // We have _numWorkTypePawnPairs such chunks in _startingSolution.
            var chunkSize = _numPriorities + 1;
            var chromosome = new int[_numWorkTypePawnPairs];

            for (var i = 0; i < _numWorkTypePawnPairs; i++)
            {
                var offset = i * chunkSize;

                // Find which index in [offset .. offset+chunkSize-1] has the largest value
                var bestIndex = 0;
                var bestValue = double.NegativeInfinity;

                for (var j = 0; j < chunkSize; j++)
                {
                    double val = _startingSolution[offset + j];
                    if (val > bestValue)
                    {
                        bestValue = val;
                        bestIndex = j;
                    }
                }

                // bestIndex is the chosen priority for pair i
                chromosome[i] = bestIndex;
            }

            return chromosome;
        }

        /// <summary>
        ///     Convert a chromosome (int[] of length = #_pairs, each in [0.._numPriorities])
        ///     to a double[] for EvaluateSolution.
        /// </summary>
        private double[] ChromosomeToDoubleArray(int[] chromosome)
        {
            // We have (#pairs) chunks, each chunk size = (_numPriorities + 1).
            // Initialize a big zero array for all bits.
            var x = new double[chromosome.Length * (_numPriorities + 1)];

            for (var i = 0; i < chromosome.Length; i++)
            {
                var chosenPriority = chromosome[i];

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
        private double Fitness(int[] chromosome, out bool isFeasible)
        {
            var x = ChromosomeToDoubleArray(chromosome);
            var (feasible, cost) = _evaluateFunc(x);

            isFeasible = feasible;

            return isFeasible ? cost : _infeasiblePenalty; // lower is better
        }

        /// <summary>
        ///     Mutate a chromosome by picking exactly 1 random gene
        ///     and setting it to a random priority in [0.._numPriorities].
        ///     If you want multiple mutations per chromosome or a different mutation approach,
        ///     you can tweak it here.
        /// </summary>
        private void Mutate(int[] chrom, double mutationRate)
        {
            if (_random.NextDouble() < mutationRate)
            {
                // pick a random gene
                var idx = _random.Next(0, chrom.Length);
                // set it to a new random priority
                chrom[idx] = _random.Next(0, _numPriorities + 1);
            }
        }

        /// <summary>
        ///     Uniform crossover: for each gene, pick from parent1 or parent2 with 50% chance.
        ///     If random > crossoverRate, we just clone parents.
        /// </summary>
        private (int[], int[]) UniformCrossover(int[] parent1, int[] parent2, double rate)
        {
            if (_random.NextDouble() > rate)
            {
                // No crossover => return clones
                return (parent1.ToArray(), parent2.ToArray());
            }

            var length = parent1.Length;
            var child1 = new int[length];
            var child2 = new int[length];

            for (var i = 0; i < length; i++)
                if (_random.NextDouble() < 0.5)
                {
                    child1[i] = parent1[i];
                    child2[i] = parent2[i];
                }
                else
                {
                    child1[i] = parent2[i];
                    child2[i] = parent1[i];
                }

            return (child1, child2);
        }

        /// <summary>
        ///     Simple tournament selection.
        ///     We pick N random solutions, pick the best (lowest fitness).
        /// </summary>
        private int[] TournamentSelection(List<(int[] chrom, double fitness, bool isFeasible)> scoredPopulation, int tournamentSize = 3)
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
    }
}
