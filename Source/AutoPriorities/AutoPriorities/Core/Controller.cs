using HarmonyLib;
using System.Reflection;
using HugsLib;
using HugsLib.Settings;
using UnityEngine;
using Verse;

namespace AutoPriorities.Core
{
    public class Controller : ModBase
    {
        public override void Initialize()
        {
            base.Initialize();
            HarmonyInst.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static SettingHandle<float> PassionMult { get; private set; }

        public override void DefsLoaded()
        {
            base.DefsLoaded();
            PassionMult = Settings.GetHandle("passionMult", "Passion multiplier",
                "Determines the importance of passions whe assigning priorities", 1f);
        }
    }
}