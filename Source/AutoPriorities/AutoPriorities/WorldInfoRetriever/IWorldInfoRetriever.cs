using System.Collections.Generic;
using AutoPriorities.Wrappers;

namespace AutoPriorities.WorldInfoRetriever
{
    public interface IWorldInfoRetriever
    {
        double PassionMultiplier { get; }

        IEnumerable<IWorkTypeWrapper> WorkTypeDefsInPriorityOrder();

        IEnumerable<IPawnWrapper> PawnsInPlayerFaction();
    }
}
