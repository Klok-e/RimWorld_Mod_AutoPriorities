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

        public IEnumerable<IWorkTypeWrapper> GetWorkTypeDefsInPriorityOrder()
        {
            return WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder.Select(x => new WorkTypeWrapper(x));
        }

        public IEnumerable<IPawnWrapper> GetAdultPawnsInPlayerFactionInCurrentMap()
        {
            return PlayerPawnsDisplayOrderUtility.InOrder(Find.CurrentMap.mapPawns.FreeColonists)
                .Where(pawn => !pawn.DevelopmentalStage.Baby())
                .Select(x => new PawnWrapper(x));
        }

        public IEnumerable<IPawnWrapper> GetAllAdultPawnsInPlayerFaction()
        {
            var caravans = Find.WorldObjects.Caravans
                .Where(caravan => caravan.IsPlayerControlled)
                .SelectMany(caravan => caravan.PawnsListForReading)
                .Where(pawn => pawn.IsColonist || pawn.IsSlaveOfColony);
            var colonists = Find.Maps.SelectMany(x => x.mapPawns.FreeColonists);
            return caravans.Concat(colonists)
                .Where(pawn => !pawn.DevelopmentalStage.Baby())
                .Select(x => new PawnWrapper(x));
        }

        public int GetMaxPriority()
        {
            return Controller.MaxPriorityAlien ?? Controller.MaxPriority;
        }

        #endregion
    }
}
