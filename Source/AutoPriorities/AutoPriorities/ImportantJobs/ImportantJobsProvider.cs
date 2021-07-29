using System.Collections.Generic;
using System.Linq;
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
            return new[] {"Firefighter", "Patient", "PatientBedRest", "BasicWorker"}.Select(_worldInfo.StringToDef)
                .Where(def => def is not null)
                .Select(x => x!)
                .ToHashSet();
        }

        #endregion
    }
}
