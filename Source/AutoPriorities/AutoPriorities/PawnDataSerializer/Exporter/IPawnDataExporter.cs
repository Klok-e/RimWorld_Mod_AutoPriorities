using System.Collections.Generic;

namespace AutoPriorities.PawnDataSerializer.Exporter
{
    public interface IPawnDataExporter
    {
        void ExportCurrentPawnData(string name);

        void ImportPawnData(string name);

        IEnumerable<string> ListSaves();

        void DeleteSave(string name);
    }
}
