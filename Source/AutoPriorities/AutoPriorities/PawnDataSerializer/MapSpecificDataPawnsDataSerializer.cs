using AutoPriorities.APLogger;
using AutoPriorities.Core;

namespace AutoPriorities.PawnDataSerializer
{
    public class MapSpecificDataPawnsDataSerializer : IPawnsDataSerializer
    {
        private readonly ILogger _logger;
        private readonly SaveDataHandler _saveDataHandler;
        private readonly IPawnDataStringSerializer _serializer;

        public MapSpecificDataPawnsDataSerializer(ILogger logger, IPawnDataStringSerializer serializer, SaveDataHandler saveDataHandler)
        {
            _logger = logger;
            _serializer = serializer;
            _saveDataHandler = saveDataHandler;
        }

        #region IPawnsDataSerializer Members

        public SaveData? LoadSavedData()
        {
            var mapSpecificData = MapSpecificData.GetForCurrentMap();
            var worldSpecificData = WorldSpecificData.GetForCurrentWorld();

            return mapSpecificData == null || worldSpecificData == null
                ? null
                : _saveDataHandler.GetSavedData(mapSpecificData, worldSpecificData);
        }

        public void SaveData(SaveDataRequest request)
        {
            var mapData = MapSpecificData.GetForCurrentMap();
            var worldSpecificData = WorldSpecificData.GetForCurrentWorld();
            if (mapData == null || worldSpecificData == null) return;

            _saveDataHandler.SaveData(request, mapData, worldSpecificData);
        }

        #endregion
    }
}
