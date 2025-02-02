using AutoPriorities.Wrappers;

namespace AutoPriorities
{
    public struct PawnFitnessData
    {
        public IPawnWrapper Pawn { get; set; }

        public float Fitness { get; set; }

        public float SkillLevel { get; set; }

        public bool IsSkilledWorkType { get; set; }

        public bool IsOpposed { get; set; }

        public void Deconstruct(out IPawnWrapper pawn, out float fitness, out float skillLevel, out bool isOpposed, out bool isDumbWorkType)
        {
            pawn = Pawn;
            fitness = Fitness;
            skillLevel = SkillLevel;
            isOpposed = IsOpposed;
            isDumbWorkType = IsSkilledWorkType;
        }
    }
}
