using System;
using AutoPriorities.Wrappers;

namespace AutoPriorities
{
    public struct ExcludedPawnEntry : IEquatable<ExcludedPawnEntry>
    {
        public IWorkTypeWrapper WorkDef { get; set; }

        public IPawnWrapper Pawn { get; set; }

        public bool Equals(ExcludedPawnEntry other)
        {
            return WorkDef.Equals(other.WorkDef) && Pawn.Equals(other.Pawn);
        }

        public override bool Equals(object? obj)
        {
            return obj is ExcludedPawnEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            return WorkDef.GetHashCode() ^ Pawn.GetHashCode();
        }
    }
}
