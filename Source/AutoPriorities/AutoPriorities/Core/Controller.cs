using HarmonyLib;
using System.Reflection;
using HugsLib;
using HugsLib.Settings;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace AutoPriorities.Core
{
    public class Controller : ModBase
    {
        public static ModLogger Log { get; private set; }
        public static AutoPriorities_Dialog Dialog { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            HarmonyInst.PatchAll(Assembly.GetExecutingAssembly());
            Log = Logger;
            Dialog = new AutoPriorities_Dialog();
        }

        public static SettingHandle<double> PassionMult { get; private set; }

        public override void DefsLoaded()
        {
            base.DefsLoaded();
            PassionMult = Settings.GetHandle("passionMult", "Passion multiplier",
                "Determines the importance of passions whe assigning priorities", 1d);
        }
    }
}