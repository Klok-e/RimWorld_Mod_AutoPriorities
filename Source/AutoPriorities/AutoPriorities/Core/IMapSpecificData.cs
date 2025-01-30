using System.Collections.Generic;

namespace AutoPriorities.Core
{
    public interface IMapSpecificData
    {
        public List<string>? ImportantWorkTypes { get; set; }
        public byte[]? PawnsDataXml { get; set; }
        public float MinimumFitness { get; set; }
        public bool IgnoreLearningRate { get; set; }
    }
}
