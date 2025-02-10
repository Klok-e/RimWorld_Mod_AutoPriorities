using System.Collections.Generic;

namespace AutoPriorities.Core
{
    public interface IWorldSpecificData
    {
        public List<ExcludedPawnEntry> ExcludedPawns { get; set; }
    }
}
