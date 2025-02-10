using System.Collections.Generic;
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

        public SaveData GetSavedData(IMapSpecificData mapSpecificData, IWorldSpecificData worldSpecificData)
        {
            var pawnsDataXml = mapSpecificData.PawnsDataXml;

            DeserializedData? deserialized = null;
            if (pawnsDataXml != null) deserialized = _serializer.Deserialize(pawnsDataXml);

            var deserializedExcluded = deserialized?.ExcludedPawns ?? new HashSet<ExcludedPawnEntry>();

            return new SaveData
            {
                ExcludedPawns =
                    deserializedExcluded is { Count: > 0 }
                        ? deserializedExcluded
                        : mapSpecificData.ExcludedPawns is { Count: > 0 }
                            ? mapSpecificData.ExcludedPawns.ToHashSet()
                            : worldSpecificData.ExcludedPawns.ToHashSet(),
                WorkTablesData = deserialized?.WorkTablesData ?? new List<WorkTableEntry>(),
                IgnoreLearningRate = mapSpecificData.IgnoreLearningRate,
                MinimumSkillLevel = mapSpecificData.MinimumSkillLevel,
                IgnoreOppositionToWork = mapSpecificData.IgnoreOppositionToWork,
                IgnoreWorkSpeed = mapSpecificData.IgnoreWorkSpeed,
                RunOnTimer = mapSpecificData.RunOnTimer,
            };
        }

        public void SaveData(SaveDataRequest request, IMapSpecificData mapDataSaveTo, IWorldSpecificData worldSpecificDataSaveTo)
        {
            var ser = _serializer.Serialize(request);
            mapDataSaveTo.PawnsDataXml = ser;

            mapDataSaveTo.MinimumSkillLevel = request.MinimumSkillLevel;
            mapDataSaveTo.ExcludedPawns = new List<ExcludedPawnEntry>();
            worldSpecificDataSaveTo.ExcludedPawns = request.ExcludedPawns.ToList();
            mapDataSaveTo.IgnoreLearningRate = request.IgnoreLearningRate;
            mapDataSaveTo.IgnoreWorkSpeed = request.IgnoreWorkSpeed;
            mapDataSaveTo.RunOnTimer = request.RunOnTimer;
        }
    }
}
