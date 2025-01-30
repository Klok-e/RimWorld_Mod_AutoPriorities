using AutoPriorities.APLogger;
using AutoPriorities.Core;

namespace AutoPriorities.PawnDataSerializer
{
    public class MapSpecificDataPawnsDataSerializer : IPawnsDataSerializer
    {
        private readonly ILogger _logger;
        private readonly SaveDataHandler _saveDataHandler;
        private readonly IPawnDataStringSerializer _serializer;

        public MapSpecificDataPawnsDataSerializer(ILogger logger, IPawnDataStringSerializer serializer,
            SaveDataHandler saveDataHandler)
        {
            _logger = logger;
            _serializer = serializer;
            _saveDataHandler = saveDataHandler;
        }

        #region IPawnsDataSerializer Members

        public SaveData? LoadSavedData()
        {
            var mapSpecificData = MapSpecificData.GetForCurrentMap();
            return mapSpecificData == null ? null : _saveDataHandler.GetSavedData(mapSpecificData);
        }

        public void SaveData(SaveDataRequest request)
        {
            var mapData = MapSpecificData.GetForCurrentMap();
            if (mapData == null) return;

            _saveDataHandler.SaveData(request, mapData);
        }

        #endregion
    }
}
