using System;
using System.IO;

namespace AutoPriorities.PawnDataSerializer.StreamProviders
{
    internal class FileStreamProvider : StreamProvider
    {
        public override T WithStream<T>(string path, FileMode mode, Func<Stream, T> callback)
        {
            using var stream = new FileStream(path, mode);
            return callback(stream);
        }

        public override bool FileExists(string path)
        {
            return File.Exists(path);
        }
    }
}
