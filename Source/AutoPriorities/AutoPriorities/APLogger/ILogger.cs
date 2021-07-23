using System;

namespace AutoPriorities.APLogger
{
    public interface ILogger
    {
        void Err(string message);

        void Err(Exception exception);

        void Warn(string message);

        void Info(string message);
    }
}
