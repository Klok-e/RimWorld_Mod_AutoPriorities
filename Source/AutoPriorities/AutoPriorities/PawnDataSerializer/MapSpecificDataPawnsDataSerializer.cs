using AutoPriorities.Core;
using AutoPriorities.WorldInfoRetriever;

namespace AutoPriorities.PawnDataSerializer
{
    public class MapSpecificDataPawnsDataSerializer : IPawnsDataSerializer
    {
        private readonly IPawnDataStringSerializer _serializer;
        private readonly IWorldInfoRetriever _worldInfoRetriever;

        public MapSpecificDataPawnsDataSerializer(
            IWorldInfoRetriever worldInfoRetriever,
            IPawnDataStringSerializer serializer)
        {
            _worldInfoRetriever = worldInfoRetriever;
            _serializer = serializer;
        }

        #region IPawnsDataSerializer Members

        public SaveData? LoadSavedData()
        {
            var pawnsDataXml = _worldInfoRetriever.PawnsDataXml;
            if (pawnsDataXml == null) return new SaveData();

            return _serializer.Deserialize(pawnsDataXml);
        }

        public void SaveData(SaveDataRequest request)
        {
            var ser = _serializer.Serialize(request);
            _worldInfoRetriever.PawnsDataXml = ser;
        }

        #endregion
    }
}
