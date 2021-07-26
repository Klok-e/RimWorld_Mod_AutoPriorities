using System.Collections.Generic;
using System.Linq;
using AutoPriorities.Core;
using AutoPriorities.Wrappers;
using RimWorld;
using Verse;

namespace AutoPriorities.WorldInfoRetriever
{
    internal class WorldInfoRetriever : IWorldInfoRetriever
    {
        #region IWorldInfoRetriever Members

        public IEnumerable<IWorkTypeWrapper> WorkTypeDefsInPriorityOrder()
        {
            return WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder.Select(x => new WorkTypeWrapper(x));
        }

        public IEnumerable<IPawnWrapper> PawnsInPlayerFaction()
        {
            return Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
                       .Select(x => new PawnWrapper(x));
        }

        public double PassionMultiplier => Controller.PassionMult;

        #endregion
    }
}
