using System;
using System.IO;
using System.Xml.Serialization;
using AutoPriorities.APLogger;
using AutoPriorities.Core;
using AutoPriorities.WorldInfoRetriever;

namespace AutoPriorities.PawnDataSerializer
{
    public class PawnDataStringSerializer : IPawnDataStringSerializer
    {
        private readonly ILogger _logger;
        private readonly IWorldInfoFacade _worldInfoFacade;

        public PawnDataStringSerializer(ILogger logger,IWorldInfoFacade worldInfoFacade)
        {
            _logger = logger;
            _worldInfoFacade = worldInfoFacade;
        }

        public SaveData? Deserialize(byte[] xml)
        {
            var stream = new MemoryStream(xml);

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

        public byte[] Serialize(SaveDataRequest request)
        {
            var stream = new MemoryStream();

            new XmlSerializer(typeof(PercentTableSaver.Ser)).Serialize(stream,
                PercentTableSaver.Ser.Serialized((request.WorkTablesData, request.ExcludedPawns)));

            stream.Position = 0;
            return stream.ToArray();
        }
    }
}
