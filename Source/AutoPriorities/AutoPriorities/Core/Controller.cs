using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;

namespace AutoPriorities.Core
{
    public class Controller : Mod
    {
        private string _buffer;

        public static Settings Settings { get; private set; }

        public Controller(ModContentPack content) : base(content)
        {
#if DEBUG
            Harmony.DEBUG = true;
#endif
            var harmony = new Harmony("auto_priorities");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Settings = GetSettings<Settings>();
            _buffer = Settings._passionMult.ToString();
        }

        public override string SettingsCategory()
        {
            return "AutoPriorities";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);

            const string category = "Passion multiplier";

            var labelRect = new Rect(
                inRect.min,
                new Vector2(category.GetWidthCached(), 25f)
                );
            Widgets.Label(labelRect, category);

            var fieldRect = new Rect(
                labelRect.xMax + 5f,
                labelRect.yMin,
                50f,
                25f
                );
            Widgets.TextFieldNumeric(fieldRect, ref Settings._passionMult, ref _buffer);
        }
    }
}
