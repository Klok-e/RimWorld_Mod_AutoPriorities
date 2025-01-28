using System;

namespace AutoPriorities
{
    public struct ExcludedPawnEntry : IEquatable<ExcludedPawnEntry>
    {
        public string WorkDef { get; set; }

        public string PawnThingId { get; set; }

        public bool Equals(ExcludedPawnEntry other)
        {
            return WorkDef == other.WorkDef && PawnThingId == other.PawnThingId;
        }

        public override bool Equals(object? obj)
        {
            return obj is ExcludedPawnEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (WorkDef != null ? WorkDef.GetHashCode() : 0) ^
                   (PawnThingId != null ? PawnThingId.GetHashCode() : 0);
        }
    }
}
