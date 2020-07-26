using System;

namespace AutoPriorities.Core
{
    public struct Priority
    {
        public int V;

        public static implicit operator Priority(int value) => new Priority {V = value};
    }
    
    public struct JobCount
    {
        public int V;

        public static implicit operator JobCount(int value) => new JobCount {V = value};
    }

    public struct Fitness
    {
        public double V;

        public static implicit operator Fitness(double value) => new Fitness {V = value};
    } 
}