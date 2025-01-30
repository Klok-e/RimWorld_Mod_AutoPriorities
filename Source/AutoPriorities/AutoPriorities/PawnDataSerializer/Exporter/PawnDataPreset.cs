using System.Text;
using AutoPriorities.APLogger;
using AutoPriorities.Core;

namespace AutoPriorities.PawnDataSerializer.Exporter
{
    public class PawnDataPreset : IPawnDataImportable
    {
        private const string DefaultPreset = "Default";
        public static readonly string[] PresetNames = { DefaultPreset };

        private readonly ILogger _logger;
        private readonly IPawnDataStringSerializer _pawnDataStringSerializer;
        private readonly PawnsData _pawnsData;

        public PawnDataPreset(ILogger logger, IPawnDataStringSerializer pawnDataStringSerializer, PawnsData pawnsData)
        {
            _logger = logger;
            _pawnDataStringSerializer = pawnDataStringSerializer;
            _pawnsData = pawnsData;
        }

        public string FileName => DefaultPreset;

        public void ImportPawnData()
        {
            var save = _pawnDataStringSerializer.Deserialize(Encoding.UTF8.GetBytes(Resources.DefaultPreset));
            if (save == null)
                return;

#if DEBUG
            _logger.Info("Loading successful. Setting loaded data.");
#endif

            _pawnsData.SetData(
                new SaveData
                {
                    ExcludedPawns = save.ExcludedPawns,
                    WorkTablesData = save.WorkTablesData,
                    IgnoreLearningRate = false,
                    MinimumFitness = 2,
                });
        }
    }
}
