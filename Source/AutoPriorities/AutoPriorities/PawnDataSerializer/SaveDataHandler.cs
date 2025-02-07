using System.Linq;
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
                ExcludedPawns = deserialized.ExcludedPawns.Count > 0
                    ? deserialized.ExcludedPawns
                    : mapSpecificData.ExcludedPawns.ToHashSet(),
                WorkTablesData = deserialized.WorkTablesData,
                IgnoreLearningRate = mapSpecificData.IgnoreLearningRate,
                MinimumSkillLevel = mapSpecificData.MinimumSkillLevel,
                IgnoreOppositionToWork = mapSpecificData.IgnoreOppositionToWork,
                IgnoreWorkSpeed = mapSpecificData.IgnoreWorkSpeed,
            };
        }

        public void SaveData(SaveDataRequest request, IMapSpecificData mapDataSaveTo)
        {
            var ser = _serializer.Serialize(request);
            mapDataSaveTo.PawnsDataXml = ser;

            mapDataSaveTo.MinimumSkillLevel = request.MinimumSkillLevel;
            mapDataSaveTo.ExcludedPawns = request.ExcludedPawns.ToList();
            mapDataSaveTo.IgnoreLearningRate = request.IgnoreLearningRate;
            mapDataSaveTo.IgnoreWorkSpeed = request.IgnoreWorkSpeed;
        }
    }
}
