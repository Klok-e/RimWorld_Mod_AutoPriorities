using UnityEngine;
using Verse;

namespace AutoPriorities.Core
{
    public class AutoPrioritiesSettings : ModSettings
    {
        public bool annonyingDebugLogs;
        public bool debugLogs;
        public bool debugSaveTablesAndPawns;
        public int maxPriority = 4;
        public float optimizationFeasibleSolutionTimeoutSeconds = 10f;
        public float optimizationImprovementSeconds = 1f;
        public float optimizationJobsPerPawnWeight = 1f;
        public float optimizationMutationRate = 0.8f;
        public int optimizationPopulationSize = 256;
        public int timerTicks = 60000;
        public bool useOldAssignmentAlgorithm;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref maxPriority, "maxPriority", 4);
            Scribe_Values.Look(ref useOldAssignmentAlgorithm, "useOldAssignmentAlgorithm");
            Scribe_Values.Look(ref debugSaveTablesAndPawns, "debugSaveTablesAndPawns");
            Scribe_Values.Look(ref debugLogs, "debugLogs");
            Scribe_Values.Look(ref annonyingDebugLogs, "annonyingDebugLogs");
            Scribe_Values.Look(ref optimizationFeasibleSolutionTimeoutSeconds, "optimizationFeasibleSolutionTimeoutSeconds", 10f);
            Scribe_Values.Look(ref optimizationImprovementSeconds, "optimizationImprovementSeconds", 1f);
            Scribe_Values.Look(ref optimizationMutationRate, "optimizationMutationRate", 0.8f);
            Scribe_Values.Look(ref optimizationPopulationSize, "optimizationPopulationSize", 256);
            Scribe_Values.Look(ref optimizationJobsPerPawnWeight, "optimizationJobsPerPawnWeight", 1f);
            Scribe_Values.Look(ref timerTicks, "timerTicks", 60000);

            ClampValues();
        }

        public void ClampValues()
        {
            maxPriority = Mathf.Clamp(maxPriority, 1, 9);
            optimizationFeasibleSolutionTimeoutSeconds = Mathf.Clamp(optimizationFeasibleSolutionTimeoutSeconds, 0f, 120f);
            optimizationImprovementSeconds = Mathf.Clamp(optimizationImprovementSeconds, 0f, 60f);
            optimizationMutationRate = Mathf.Clamp(optimizationMutationRate, 0f, 1f);
            optimizationPopulationSize = Mathf.Clamp(optimizationPopulationSize, 2, 4096);
            optimizationJobsPerPawnWeight = Mathf.Max(0f, optimizationJobsPerPawnWeight);
            timerTicks = Mathf.Max(1, timerTicks);
        }
    }
}
