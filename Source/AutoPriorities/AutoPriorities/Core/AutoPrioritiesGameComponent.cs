using Verse;

namespace AutoPriorities.Core
{
    public class AutoPrioritiesGameComponent : GameComponent
    {
        public AutoPrioritiesGameComponent(Game game)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Controller.OnGameLoaded();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            Controller.GameTick();
        }

        public override void GameComponentUpdate()
        {
            base.GameComponentUpdate();
            Controller.ProcessDelayedActions();
        }
    }
}
