using System.Collections.Generic;
using AutoPriorities.Wrappers;

namespace AutoPriorities.WorldInfoRetriever
{
    public interface IWorldInfoRetriever
    {
        IEnumerable<IWorkTypeWrapper> GetWorkTypeDefsInPriorityOrder();

        IEnumerable<IPawnWrapper> GetAdultPawnsInPlayerFactionInCurrentMap();

        IEnumerable<IPawnWrapper> GetAllAdultPawnsInPlayerFaction();

        double GetMinimumWorkFitness();

        int GetMaxPriority();

        byte[]? PawnsDataXml { get; set; }
    }
}
