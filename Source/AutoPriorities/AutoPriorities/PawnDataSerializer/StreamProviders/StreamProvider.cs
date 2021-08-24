using System;
using System.IO;
using AutoPriorities.Core;

namespace AutoPriorities.PawnDataSerializer.StreamProviders
{
    // until default interface impls are supported by target
    public abstract class StreamProvider
    {
        public void WithStream(string path, FileMode mode, Action<Stream> callback)
        {
            WithStream<Unit>(
                path,
                mode,
                stream =>
                {
                    callback(stream);
                    return default;
                });
        }

        public abstract T WithStream<T>(string path, FileMode mode, Func<Stream, T> callback);

        public abstract bool FileExists(string path);
    }
}
