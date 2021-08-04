using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using AutoPriorities.APLogger;
using AutoPriorities.Core;
using AutoPriorities.WorldInfoRetriever;

namespace AutoPriorities.PawnDataSerializer
{
    public class MapSpecificDataPawnsDataSerializer : IPawnsDataSerializer
    {
        private readonly ILogger _logger;
        private readonly IWorldInfoFacade _worldInfoFacade;

        public MapSpecificDataPawnsDataSerializer(ILogger logger, IWorldInfoFacade worldInfoFacade)
        {
            _logger = logger;
            _worldInfoFacade = worldInfoFacade;
        }

        #region IPawnsDataSerializer Members

        public SaveData? LoadSavedData()
        {
            var mapData = MapSpecificData.GetForCurrentMap();
            if (mapData == null) return new SaveData();

            if (mapData.pawnsDataXml == null) return new SaveData();

            var stream = new MemoryStream(mapData.pawnsDataXml);

            try
            {
                var ser = (PercentTableSaver.Ser)new XmlSerializer(typeof(PercentTableSaver.Ser)).Deserialize(stream);
                var workTableEntries = ser.ParsedData(_worldInfoFacade);
                var excludedPawnEntries = ser.ParsedExcluded(_worldInfoFacade);
                return new SaveData { ExcludedPawns = excludedPawnEntries, WorkTablesData = workTableEntries };
            }
            catch (Exception e)
            {
                _logger.Err("Error while deserializing state");
                _logger.Err(e);
            }

            return null;
        }

        public void SaveData(SaveDataRequest request)
        {
            var mapData = MapSpecificData.GetForCurrentMap();
            if (mapData == null) return;

            var stream = new MemoryStream();

            new XmlSerializer(typeof(PercentTableSaver.Ser)).Serialize(stream,
                PercentTableSaver.Ser.Serialized((request.WorkTablesData, request.ExcludedPawns)));

            stream.Position = 0;
            mapData.pawnsDataXml = stream.ToArray();
        }

        #endregion
    }
}
