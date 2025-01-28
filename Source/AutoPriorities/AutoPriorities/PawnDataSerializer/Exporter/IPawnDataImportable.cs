namespace AutoPriorities.PawnDataSerializer.Exporter
{
    public interface IPawnDataImportable
    {
        public string FileName { get; }

        public void ImportPawnData();
    }
}
