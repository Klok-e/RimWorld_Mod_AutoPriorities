using AutoPriorities.APLogger;

namespace AutoPriorities.PawnDataSerializer
{
    public class CombinedPawnsDataSerializer : IPawnsDataSerializer
    {
        private readonly ILogger _logger;
        private readonly MapSpecificDataPawnsDataSerializer _mapSpecificSerializer;

        public CombinedPawnsDataSerializer(ILogger logger,
            MapSpecificDataPawnsDataSerializer mapSpecificSerializer)
        {
            _logger = logger;
            _mapSpecificSerializer = mapSpecificSerializer;
        }

        #region IPawnsDataSerializer Members

        public SaveData? LoadSavedData()
        {
            var mapSpecific = _mapSpecificSerializer.LoadSavedData();
#if DEBUG
            _logger.Info("Map specific data loaded.");
#endif
            return mapSpecific;
        }

        public void SaveData(SaveDataRequest request)
        {
            _mapSpecificSerializer.SaveData(request);
        }

        #endregion
    }
}
