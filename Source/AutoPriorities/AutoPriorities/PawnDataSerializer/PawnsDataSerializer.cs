using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using AutoPriorities.APLogger;
using AutoPriorities.Core;
using AutoPriorities.PawnDataSerializer.StreamProviders;
using AutoPriorities.WorldInfoRetriever;

namespace AutoPriorities.PawnDataSerializer
{
    public class PawnsDataSerializer : IPawnsDataSerializer
    {
        private readonly string _fullPath;
        private readonly ILogger _logger;
        private readonly StreamProvider _streamProvider;
        private readonly IWorldInfoFacade _worldInfo;

        public PawnsDataSerializer(ILogger logger,
            string fullPath,
            IWorldInfoFacade worldInfo,
            StreamProvider streamProvider)
        {
            _logger = logger;
            _fullPath = fullPath;
            _worldInfo = worldInfo;
            _streamProvider = streamProvider;
        }

        #region IPawnsDataSerializer Members

        public SaveData LoadSavedData()
        {
            var (percents, excluded) = GetStateLoaders();

            return new SaveData {ExcludedPawns = excluded, WorkTablesData = percents};
        }

        public void SaveData(SaveDataRequest request)
        {
            SaveState((request.WorkTablesData, request.ExcludedPawns));
        }

        #endregion

        private void SaveState((List<WorkTableEntry>, HashSet<ExcludedPawnEntry>) state)
        {
            _streamProvider.WithStream(_fullPath, FileMode.Create, stream =>
            {
                new XmlSerializer(typeof(PercentTableSaver.Ser)).Serialize(stream,
                    PercentTableSaver.Ser.Serialized(state));
            });
        }

        private (List<WorkTableEntry> percents, HashSet<ExcludedPawnEntry> excluded) GetStateLoaders()
        {
            try
            {
                if (_streamProvider.FileExists(_fullPath))
                    return _streamProvider.WithStream(_fullPath, FileMode.OpenOrCreate, stream =>
                    {
                        var ser =
                            (PercentTableSaver.Ser)new XmlSerializer(typeof(PercentTableSaver.Ser)).Deserialize(stream);
                        return (ser.ParsedData(_worldInfo), ser.ParsedExcluded(_worldInfo));
                    });
            }
            catch (Exception e)
            {
                _logger.Err("Error while deserializing state");
                _logger.Err(e);
            }

            return (new List<WorkTableEntry>(), new HashSet<ExcludedPawnEntry>());
        }
    }
}
