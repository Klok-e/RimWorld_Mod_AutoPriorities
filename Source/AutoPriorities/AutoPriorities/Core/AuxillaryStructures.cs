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
}