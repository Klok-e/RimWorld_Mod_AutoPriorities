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
            return PlayerPawnsDisplayOrderUtility.InOrder(Find.CurrentMap.mapPawns.FreeColonists)
                .Select(x => new PawnWrapper(x));
        }

        public byte[]? PawnsDataXml
        {
            get => MapSpecificData.GetForCurrentMap()?.pawnsDataXml;
            set
            {
                var mapSpecificData = MapSpecificData.GetForCurrentMap();
                if (mapSpecificData == null) return;

                mapSpecificData.pawnsDataXml = value;
            }
        }

        #endregion
    }
}
