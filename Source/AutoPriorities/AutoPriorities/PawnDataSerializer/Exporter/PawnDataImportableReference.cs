namespace AutoPriorities.PawnDataSerializer.Exporter
{
    public class PawnDataImportableReference : IPawnDataImportable
    {
        private readonly PawnDataExporter _pawnDataExporter;

        public PawnDataImportableReference(string fileName, PawnDataExporter pawnDataExporter)
        {
            _pawnDataExporter = pawnDataExporter;
            FileName = fileName;
        }

        public string FileName { get; }

        public void ImportPawnData()
        {
            _pawnDataExporter.ImportPawnData(FileName);
        }
    }
}
