using AutoPriorities.Wrappers;

namespace AutoPriorities
{
    public struct PawnFitnessData
    {
        public IPawnWrapper Pawn { get; set; }

        public double Fitness { get; set; }

        public double SkillLevel { get; set; }

        public bool IsDumbWorkType { get; set; }

        public bool IsOpposed { get; set; }

        public void Deconstruct(out IPawnWrapper pawn, out double fitness, out double skillLevel, out bool isOpposed,
            out bool isDumbWorkType)
        {
            pawn = Pawn;
            fitness = Fitness;
            skillLevel = SkillLevel;
            isOpposed = IsOpposed;
            isDumbWorkType = IsDumbWorkType;
        }
    }
}
