using System.Collections.Generic;
using AutoPriorities.Wrappers;

namespace AutoPriorities.WorldInfoRetriever
{
    public interface IWorldInfoRetriever
    {
        IEnumerable<IWorkTypeWrapper> WorkTypeDefsInPriorityOrder();

        IEnumerable<IPawnWrapper> PawnsInPlayerFaction();

        byte[]? PawnsDataXml { get; set; }
    }
}
