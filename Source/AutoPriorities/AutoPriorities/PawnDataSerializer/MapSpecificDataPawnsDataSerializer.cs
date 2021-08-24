using AutoPriorities.APLogger;
using AutoPriorities.Core;
using AutoPriorities.WorldInfoRetriever;

namespace AutoPriorities.PawnDataSerializer
{
    public class MapSpecificDataPawnsDataSerializer : IPawnsDataSerializer
    {
        private readonly ILogger _logger;
        private readonly IPawnDataStringSerializer _serializer;
        private readonly IWorldInfoFacade _worldInfoFacade;

        public MapSpecificDataPawnsDataSerializer(ILogger logger,
            IWorldInfoFacade worldInfoFacade,
            IPawnDataStringSerializer serializer)
        {
            _logger = logger;
            _worldInfoFacade = worldInfoFacade;
            _serializer = serializer;
        }

        #region IPawnsDataSerializer Members

        public SaveData? LoadSavedData()
        {
            var mapData = MapSpecificData.GetForCurrentMap();
            if (mapData == null) return new SaveData();

            if (mapData.pawnsDataXml == null) return new SaveData();

            return _serializer.Deserialize(mapData.pawnsDataXml);
        }

        public void SaveData(SaveDataRequest request)
        {
            var mapData = MapSpecificData.GetForCurrentMap();
            if (mapData == null) return;

            var ser = _serializer.Serialize(request);
            mapData.pawnsDataXml = ser;
        }

        #endregion
    }
}
