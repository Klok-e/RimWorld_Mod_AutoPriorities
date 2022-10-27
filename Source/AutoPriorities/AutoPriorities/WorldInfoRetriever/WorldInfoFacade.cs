using System.Linq;
using AutoPriorities.APLogger;
using AutoPriorities.Core;
using AutoPriorities.Wrappers;

namespace AutoPriorities.WorldInfoRetriever
{
    public class WorldInfoFacade : IWorldInfoFacade
    {
        private readonly ILogger _logger;
        private readonly IWorldInfoRetriever _worldInfo;

        public WorldInfoFacade(IWorldInfoRetriever worldInfo, ILogger logger)
        {
            _worldInfo = worldInfo;
            _logger = logger;
        }

        #region IWorldInfoFacade Members

        public IWorkTypeWrapper? StringToDef(string name)
        {
            var work = _worldInfo.WorkTypeDefsInPriorityOrder()
                .FirstOrDefault(w => w.DefName == name);
            if (work is not null) return work;

            _logger.Warn($"Work type {name} not found. Excluding {name} from the internal data structure.");
            return null;
        }

        public IPawnWrapper? IdToPawn(string pawnId)
        {
            var res = _worldInfo.PawnsInPlayerFactionInCurrentMap()
                .FirstOrDefault(p => p.ThingID == pawnId);
            if (res is not null) return res;

            _logger.Warn($"pawn {pawnId} wasn't found while deserializing data, skipping...");
            return null;
        }

        #endregion
    }
}
