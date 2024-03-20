using Verse;

namespace AutoPriorities.PawnDataSerializer.Exporter
{
    public class SavedPawnDataReference : IRenameable
    {
        private readonly IPawnDataExporter _pawnDataExporter;

        public string FileName { get; set; }

        public const string StartingName = "AutoPriorities";

        public SavedPawnDataReference(IPawnDataExporter pawnDataExporter, string fileName)
        {
            _pawnDataExporter = pawnDataExporter;
            FileName = fileName;
        }

        public string RenamableLabel
        {
            get => FileName;
            set
            {
                _pawnDataExporter.RenameFile(RenamableLabel, value);
                FileName = value;
            }
        }

        public string BaseLabel => StartingName;

        public string InspectLabel => RenamableLabel;
    }
}
