using System;
using UnityEngine;

namespace AutoPriorities.Core
{
    [Serializable]
    public struct Priority : IEquatable<Priority>
    {
        [SerializeField] public int v;

        public static implicit operator Priority(int value)
        {
            return new Priority { v = value };
        }

        public bool Equals(Priority other)
        {
            return v == other.v;
        }

        public override bool Equals(object? obj)
        {
            return obj is Priority other && Equals(other);
        }

        public override int GetHashCode()
        {
            return v;
        }
    }

    [Serializable]
    public struct JobCount
    {
        [SerializeField] public int v;

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
