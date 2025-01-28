using System.IO;

namespace AutoPriorities.PawnDataSerializer.Exporter
{
    public class SaveFilePather
    {
        private const string Extension = ".xml";
        private readonly string _saveDirectoryPath;

        public SaveFilePather(string saveDirectoryPath)
        {
            _saveDirectoryPath = saveDirectoryPath;
        }

        public string FullPath(string name)
        {
            var nameWithExt = Path.ChangeExtension(name, Extension);
            return Path.Combine(_saveDirectoryPath, nameWithExt);
        }
    }
}
