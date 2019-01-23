using Verse;

namespace AutoPriorities.Core
{
    public class Settings : ModSettings
    {
        public float _passionMult;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _passionMult, "_passionMult", 0.1f);
        }
    }
}
