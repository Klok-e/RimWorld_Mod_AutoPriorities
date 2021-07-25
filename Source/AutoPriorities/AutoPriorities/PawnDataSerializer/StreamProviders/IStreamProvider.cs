using System;
using System.IO;

namespace AutoPriorities.PawnDataSerializer
{
    public interface IStreamProvider
    {
        void WithStream(string path, FileMode mode, Action<Stream> callback);

        T WithStream<T>(string path, FileMode mode, Func<Stream, T> callback);

        bool FileExists(string path);
    }
}
