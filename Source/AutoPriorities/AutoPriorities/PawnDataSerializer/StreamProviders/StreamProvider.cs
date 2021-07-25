using System;
using System.IO;
using AutoPriorities.Core;

namespace AutoPriorities.PawnDataSerializer
{
    internal class StreamProvider : IStreamProvider
    {
        #region IStreamProvider Members

        public void WithStream(string path, FileMode mode, Action<Stream> callback)
        {
            WithStream<Unit>(path, mode, stream =>
            {
                callback(stream);
                return default;
            });
        }

        public T WithStream<T>(string path, FileMode mode, Func<Stream, T> callback)
        {
            using var stream = new FileStream(path, mode);
            return callback(stream);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        #endregion
    }
}
