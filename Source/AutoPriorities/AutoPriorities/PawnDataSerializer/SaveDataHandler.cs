using AutoPriorities.APLogger;
using AutoPriorities.Core;

namespace AutoPriorities.PawnDataSerializer
{
    public class SaveDataHandler
    {
        private readonly ILogger _logger;
        private readonly IPawnDataStringSerializer _serializer;

        public SaveDataHandler(ILogger logger, IPawnDataStringSerializer serializer)
        {
            _logger = logger;
            _serializer = serializer;
        }

        public SaveData? GetSavedData(IMapSpecificData mapSpecificData)
        {
            var pawnsDataXml = mapSpecificData.PawnsDataXml;
            if (pawnsDataXml == null)
                return null;

            var deserialized = _serializer.Deserialize(pawnsDataXml);

            if (deserialized == null)
                return null;

            return new SaveData
            {
                ExcludedPawns = deserialized.ExcludedPawns,
                WorkTablesData = deserialized.WorkTablesData,
                IgnoreLearningRate = mapSpecificData.IgnoreLearningRate,
                MinimumSkillLevel = mapSpecificData.MinimumSkillLevel,
                IgnoreOppositionToWork = mapSpecificData.IgnoreOppositionToWork,
            };
        }

        public void SaveData(SaveDataRequest request, IMapSpecificData mapDataSaveTo)
        {
            var ser = _serializer.Serialize(request);
            mapDataSaveTo.PawnsDataXml = ser;

            mapDataSaveTo.MinimumSkillLevel = request.MinimumSkillLevel;
            mapDataSaveTo.IgnoreLearningRate = request.IgnoreLearningRate;
            mapDataSaveTo.IgnoreOppositionToWork = request.IgnoreOppositionToWork;
        }
    }
}
