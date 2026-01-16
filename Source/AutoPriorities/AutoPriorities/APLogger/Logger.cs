using System;
using Verse;

namespace AutoPriorities.APLogger
{
    public class Logger : ILogger
    {
        #region ILogger Members

        public void Err(string message)
        {
            Log.Error(message);
        }

        public void Err(Exception exception)
        {
            Log.Error(exception.ToString());
        }

        public void Warn(string message)
        {
            Log.Warning(message);
        }

        public void Info(string message)
        {
            Log.Message(message);
        }

        #endregion
    }
}
