using Verse;

namespace AutoPriorities.PawnDataSerializer
{
    public class ExcludedPawnSerializableEntry : IExposable
    {
        public Pawn? pawn;
        public WorkTypeDef? workTypeDef;

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Defs.Look(ref workTypeDef, "workTypeDef");
        }
    }
}
