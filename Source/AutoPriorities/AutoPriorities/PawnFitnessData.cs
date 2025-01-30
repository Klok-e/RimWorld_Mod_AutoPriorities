using AutoPriorities.Wrappers;

namespace AutoPriorities
{
    public struct PawnFitnessData
    {
        public IPawnWrapper Pawn { get; set; }

        public double Fitness { get; set; }

        public double SkillLevel { get; set; }

        public void Deconstruct(out IPawnWrapper pawn, out double fitness, out double skillLevel)
        {
            pawn = Pawn;
            fitness = Fitness;
            skillLevel = SkillLevel;
        }
    }
}
