using AutoPriorities.APLogger;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.WorldInfoRetriever;

namespace AutoPriorities
{
    public class PawnsDataBuilder
    {
        private readonly ILogger _logger;
        private readonly IPawnsDataSerializer _serializer;
        private readonly IWorkSpeedCalculator _workSpeedCalculator;
        private readonly IWorldInfoRetriever _worldInfoRetriever;

        public PawnsDataBuilder(IPawnsDataSerializer serializer, IWorldInfoRetriever worldInfoRetriever, ILogger logger,
            IWorkSpeedCalculator workSpeedCalculator)
        {
            _serializer = serializer;
            _worldInfoRetriever = worldInfoRetriever;
            _logger = logger;
            _workSpeedCalculator = workSpeedCalculator;
        }

        public PawnsData Build()
        {
            var data = new PawnsData(_serializer, _worldInfoRetriever, _logger, _workSpeedCalculator);
            var save = _serializer.LoadSavedData() ?? new SaveData();
            data.SetData(save);
            return data;
        }

        public void Build(PawnsData destination)
        {
            var save = _serializer.LoadSavedData() ?? new SaveData();
            destination.SetData(save);
        }
    }
}
