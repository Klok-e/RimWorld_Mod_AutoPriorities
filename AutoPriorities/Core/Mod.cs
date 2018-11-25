using Harmony;
using System.Reflection;
using Verse;

namespace AutoPriorities.Core
{
    public class Controller : Mod
    {
        public Controller(ModContentPack content) : base(content)
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            var harmony = HarmonyInstance.Create("auto_priorities");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
