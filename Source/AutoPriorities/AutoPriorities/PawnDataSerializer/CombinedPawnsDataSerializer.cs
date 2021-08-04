using AutoPriorities.APLogger;

namespace AutoPriorities.PawnDataSerializer
{
    public class CombinedPawnsDataSerializer : IPawnsDataSerializer
    {
        private readonly ILogger _logger;
        private readonly MapSpecificDataPawnsDataSerializer _mapSpecificSerializer;
        private readonly PawnsDataSerializer _pawnsDataSerializer;

        public CombinedPawnsDataSerializer(ILogger logger,
            MapSpecificDataPawnsDataSerializer mapSpecificSerializer,
            PawnsDataSerializer pawnsDataSerializer)
        {
            _logger = logger;
            _mapSpecificSerializer = mapSpecificSerializer;
            _pawnsDataSerializer = pawnsDataSerializer;
        }

        #region IPawnsDataSerializer Members

        public SaveData? LoadSavedData()
        {
            var fileSer = _pawnsDataSerializer.LoadSavedData();
            if (fileSer != null)
            {
                _pawnsDataSerializer.DeleteSaveFile();
                _logger.Info("Save file migrated successfully");
                return fileSer;
            }

            var mapSpecific = _mapSpecificSerializer.LoadSavedData();
            return mapSpecific;
        }

        public void SaveData(SaveDataRequest request)
        {
            _mapSpecificSerializer.SaveData(request);
        }

        #endregion
    }
}
