using System.Collections.Generic;
using System.Linq;
using AutoPriorities.Core;
using AutoPriorities.WorldInfoRetriever;
using AutoPriorities.Wrappers;

namespace AutoPriorities.ImportantJobs
{
    public class ImportantJobsProvider : IImportantJobsProvider
    {
        private readonly IWorldInfoFacade _worldInfo;

        public ImportantJobsProvider(IWorldInfoFacade worldInfo)
        {
            _worldInfo = worldInfo;
        }

        #region IImportantJobsProvider Members

        public HashSet<IWorkTypeWrapper> ImportantWorkTypes()
        {
            return MapSpecificData.GetForCurrentMap()
                                  ?.importantWorks.Select(_worldInfo.StringToDef)
                                  .Where(def => def is not null)
                                  .Select(x => x!)
                                  .ToHashSet() ?? new HashSet<IWorkTypeWrapper>();
        }

        public void SaveImportantWorkTypes(IEnumerable<string> workTypeDefNames)
        {
            var map = MapSpecificData.GetForCurrentMap();
            if (map == null) return;

            map.importantWorks.Clear();
            map.importantWorks.AddRange(workTypeDefNames);
        }

        #endregion
    }
}
