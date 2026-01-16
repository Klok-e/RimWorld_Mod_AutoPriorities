using System.Collections.Generic;
using System.Linq;
using AutoPriorities.Core;

namespace AutoPriorities.PawnDataSerializer
{
    public class SaveDataHandler
    {
        private readonly IPawnDataStringSerializer _serializer;

        public SaveDataHandler(IPawnDataStringSerializer serializer)
        {
            _serializer = serializer;
        }

        public SaveData GetSavedData(IMapSpecificData mapSpecificData, IWorldSpecificData worldSpecificData)
        {
            var pawnsDataXml = mapSpecificData.PawnsDataXml;

            DeserializedData? deserialized = null;
            if (pawnsDataXml != null) deserialized = _serializer.Deserialize(pawnsDataXml);

            return new SaveData
            {
                ExcludedPawns = worldSpecificData.ExcludedPawns.ToHashSet(),
                WorkTablesData = deserialized?.WorkTablesData ?? new List<WorkTableEntry>(),
                IgnoreLearningRate = mapSpecificData.IgnoreLearningRate,
                MinimumSkillLevel = mapSpecificData.MinimumSkillLevel,
                IgnoreOppositionToWork = mapSpecificData.IgnoreOppositionToWork,
                IgnoreDownedStatus = mapSpecificData.IgnoreDownedStatus,
                IgnoreWorkSpeed = mapSpecificData.IgnoreWorkSpeed,
                RunOnTimer = mapSpecificData.RunOnTimer,
            };
        }

        public void SaveData(SaveDataRequest request, IMapSpecificData mapDataSaveTo, IWorldSpecificData worldSpecificDataSaveTo)
        {
            var ser = _serializer.Serialize(request);
            mapDataSaveTo.PawnsDataXml = ser;

            mapDataSaveTo.MinimumSkillLevel = request.MinimumSkillLevel;
            worldSpecificDataSaveTo.ExcludedPawns = request.ExcludedPawns.ToList();
            mapDataSaveTo.IgnoreLearningRate = request.IgnoreLearningRate;
            mapDataSaveTo.IgnoreOppositionToWork = request.IgnoreOppositionToWork;
            mapDataSaveTo.IgnoreWorkSpeed = request.IgnoreWorkSpeed;
            mapDataSaveTo.IgnoreDownedStatus = request.IgnoreDownedStatus;
            mapDataSaveTo.RunOnTimer = request.RunOnTimer;
        }
    }
}
