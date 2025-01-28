using System.Collections.Generic;
using AutoPriorities.Wrappers;

namespace AutoPriorities.WorldInfoRetriever
{
    public interface IWorldInfoRetriever
    {
        byte[]? PawnsDataXml { get; set; }
        IEnumerable<IWorkTypeWrapper> GetWorkTypeDefsInPriorityOrder();

        IEnumerable<IPawnWrapper> GetAdultPawnsInPlayerFactionInCurrentMap();

        IEnumerable<IPawnWrapper> GetAllAdultPawnsInPlayerFaction();

        double GetMinimumWorkFitness();

        int GetMaxPriority();
    }
}
