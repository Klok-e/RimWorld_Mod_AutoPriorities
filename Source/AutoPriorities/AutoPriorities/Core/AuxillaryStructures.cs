namespace AutoPriorities.Core
{
    public struct Priority
    {
        public int V;

        public static implicit operator Priority(int value)
        {
            return new Priority {V = value};
        }
    }

    public struct JobCount
    {
        public int V;

        public static implicit operator JobCount(int value)
        {
            return new JobCount {V = value};
        }
    }

    public struct Fitness
    {
        public double V;

        public static implicit operator Fitness(double value)
        {
            return new Fitness {V = value};
        }
    }
}
