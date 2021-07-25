using System;
using System.IO;
using AutoPriorities.Core;

namespace AutoPriorities.PawnDataSerializer
{
    internal class MemoryStreamProvider : IStreamProvider
    {
        private readonly MemoryStream _stream;

        public MemoryStreamProvider(MemoryStream stream)
        {
            _stream = stream;
        }

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
            return callback(_stream);
        }

        public bool FileExists(string path)
        {
            return true;
        }

        #endregion
    }
}
