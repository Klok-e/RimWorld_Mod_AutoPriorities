using System.Collections.Generic;

namespace AutoPriorities.PawnDataSerializer.Exporter
{
    public interface IPawnDataExporter
    {
        void ExportCurrentPawnData(SavedPawnDataReference name);

        void ImportPawnData(string name);
        
        void RenameFile(string name, string newName);

        IEnumerable<SavedPawnDataReference> ListSaves();
        
        SavedPawnDataReference GetNextSavedPawnDataReference();

        void DeleteSave(string name);
    }
}
