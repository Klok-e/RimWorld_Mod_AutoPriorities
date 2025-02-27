using System.Collections.Generic;
using AutoPriorities.Wrappers;

namespace AutoPriorities.WorldInfoRetriever
{
    public interface IWorldInfoRetriever
    {
        IEnumerable<IWorkTypeWrapper> GetWorkTypeDefsInPriorityOrder();

        IEnumerable<IPawnWrapper> GetAdultPawnsInPlayerFactionInCurrentMap();

        IEnumerable<IPawnWrapper> GetAllAdultPawnsInPlayerFaction();

        int GetMaxPriority();

        bool GetUseOldAssignmentAlgorithm();

        bool DebugSaveTablesAndPawns();

        bool DebugLogs();

        float OptimizationFeasibleSolutionTimeoutSeconds();

        float OptimizationImprovementSeconds();

        float OptimizationMutationRate();

        int OptimizationPopulationSize();

        float OptimizationJobsPerPawnWeight();
    }
}
