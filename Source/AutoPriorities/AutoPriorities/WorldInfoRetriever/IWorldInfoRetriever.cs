using System.Collections.Generic;
using AutoPriorities.Wrappers;

namespace AutoPriorities.WorldInfoRetriever
{
    public interface IWorldInfoRetriever
    {
        IEnumerable<IWorkTypeWrapper> WorkTypeDefsInPriorityOrder();

        IEnumerable<IPawnWrapper> AdultPawnsInPlayerFactionInCurrentMap();

        IEnumerable<IPawnWrapper> AllAdultPawnsInPlayerFaction();

        double MinimumWorkFitness();

        byte[]? PawnsDataXml { get; set; }
    }
}
