using System.Collections.Generic;
using AutoPriorities.Wrappers;

namespace AutoPriorities.ImportantJobs
{
    public interface IImportantJobsProvider
    {
        HashSet<IWorkTypeWrapper> ImportantWorkTypes();

        void SaveImportantWorkTypes(IEnumerable<string> workTypeDefNames);
    }
}
