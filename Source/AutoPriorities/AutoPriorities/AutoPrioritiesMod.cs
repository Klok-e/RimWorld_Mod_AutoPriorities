using System.Collections.Generic;
using System.Globalization;
using AutoPriorities.Core;
using AutoPriorities.Ui;
using UnityEngine;
using Verse;

namespace AutoPriorities
{
    public class AutoPrioritiesMod : Mod
    {
        private static readonly Dictionary<string, string> NumericBuffers = new();

        public AutoPrioritiesMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<AutoPrioritiesSettings>();
            Settings.ClampValues();
        }

        public static AutoPrioritiesSettings? Settings { get; private set; }

        public override string SettingsCategory()
        {
            return "Auto Priorities";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (Settings == null)
                return;

            var oldTimerTicks = Settings.timerTicks;

            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("Max priority");
            var maxPriorityRect = listing.GetRect(24f);
            DrawNumericField(maxPriorityRect, ref Settings.maxPriority, 1, 9, "AutoPriorities.MaxPriority");

            listing.GapLine();

            listing.CheckboxLabeled("Use old assignment algorithm", ref Settings.useOldAssignmentAlgorithm);
            listing.CheckboxLabeled("Debug save tables and pawns", ref Settings.debugSaveTablesAndPawns);
            listing.CheckboxLabeled("Debug logs", ref Settings.debugLogs);
            listing.CheckboxLabeled("Annoying debug logs", ref Settings.annonyingDebugLogs);

            listing.GapLine();

            listing.Label("Optimization feasible solution timeout (seconds)");
            var feasibleTimeoutRect = listing.GetRect(24f);
            DrawNumericField(
                feasibleTimeoutRect,
                ref Settings.optimizationFeasibleSolutionTimeoutSeconds,
                0f,
                120f,
                "AutoPriorities.OptimizationFeasibleSolutionTimeoutSeconds"
            );

            listing.Label("Optimization improvement seconds");
            var improveSecondsRect = listing.GetRect(24f);
            DrawNumericField(
                improveSecondsRect,
                ref Settings.optimizationImprovementSeconds,
                0f,
                60f,
                "AutoPriorities.OptimizationImprovementSeconds"
            );

            listing.Label("Optimization mutation rate");
            var mutationRateRect = listing.GetRect(24f);
            DrawNumericField(mutationRateRect, ref Settings.optimizationMutationRate, 0f, 1f, "AutoPriorities.OptimizationMutationRate");

            listing.Label("Optimization population size");
            var populationSizeRect = listing.GetRect(24f);
            DrawNumericField(
                populationSizeRect,
                ref Settings.optimizationPopulationSize,
                2,
                4096,
                "AutoPriorities.OptimizationPopulationSize"
            );

            listing.Label("Optimization jobs per pawn weight");
            var jobsWeightRect = listing.GetRect(24f);
            DrawNumericField(
                jobsWeightRect,
                ref Settings.optimizationJobsPerPawnWeight,
                0f,
                1000f,
                "AutoPriorities.OptimizationJobsPerPawnWeight"
            );

            listing.GapLine();

            listing.Label("Timer ticks (default 60000)");
            var timerTicksRect = listing.GetRect(24f);
            DrawNumericField(timerTicksRect, ref Settings.timerTicks, 1, 2000000, "AutoPriorities.TimerTicks");

            listing.GapLine();
            listing.Label("Jobs forbidden for non-adults");
            if (listing.ButtonText(Consts.ConfigureNonAdultWorkTypes))
                Find.WindowStack.Add(new NonAdultForbiddenWorkTypesDialog());

            listing.End();

            Settings.ClampValues();

            if (Settings.timerTicks != oldTimerTicks)
                Controller.SetupPrioritiesOnTimerIfNeeded();
        }

        private static void DrawNumericField(Rect rect, ref int value, int min, int max, string controlName)
        {
            GUI.SetNextControlName(controlName);
            var buffer = GetNumericBuffer(controlName, value);
            Widgets.TextFieldNumeric(rect, ref value, ref buffer, min, max);
            UpdateNumericBuffer(controlName, buffer);
        }

        private static void DrawNumericField(Rect rect, ref float value, float min, float max, string controlName)
        {
            GUI.SetNextControlName(controlName);
            var buffer = GetNumericBuffer(controlName, value);
            Widgets.TextFieldNumeric(rect, ref value, ref buffer, min, max);
            UpdateNumericBuffer(controlName, buffer);
        }

        private static string GetNumericBuffer(string controlName, int value)
        {
            if (NumericBuffers.TryGetValue(controlName, out var buffer))
                return buffer;

            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string GetNumericBuffer(string controlName, float value)
        {
            if (NumericBuffers.TryGetValue(controlName, out var buffer))
                return buffer;

            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static void UpdateNumericBuffer(string controlName, string buffer)
        {
            if (GUI.GetNameOfFocusedControl() == controlName)
                NumericBuffers[controlName] = buffer;
            else
                NumericBuffers.Remove(controlName);
        }
    }
}
