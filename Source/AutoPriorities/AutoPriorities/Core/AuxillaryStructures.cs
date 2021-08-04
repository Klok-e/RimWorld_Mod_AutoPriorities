namespace AutoPriorities.Core
{
    public struct Priority
    {
        public int v;

        public static implicit operator Priority(int value)
        {
            return new Priority { v = value };
        }
    }

    public struct JobCount
    {
        public int v;

        public static implicit operator JobCount(int value)
        {
            return new JobCount { v = value };
        }
    }

    public struct Fitness
    {
        public double v;

        public static implicit operator Fitness(double value)
        {
            return new Fitness { v = value };
        }
    }
}
