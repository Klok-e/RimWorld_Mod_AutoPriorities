using Verse;

namespace AutoPriorities.PawnDataSerializer.Exporter
{
    public class SavedPawnDataRenameableReference : IRenameable
    {
        public const string StartingName = "AutoPriorities";

        private readonly PawnDataExporter _pawnDataExporter;

        public SavedPawnDataRenameableReference(PawnDataExporter pawnDataExporter, string fileName)
        {
            _pawnDataExporter = pawnDataExporter;
            FileName = fileName;
        }

        public string FileName { get; set; }

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
