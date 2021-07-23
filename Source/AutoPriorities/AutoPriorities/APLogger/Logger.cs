using System;
using AutoPriorities.Core;

namespace AutoPriorities.APLogger
{
    public class Logger : ILogger
    {
        #region ILogger Members

        public void Err(string message)
        {
            Controller.Log?.Error(message);
        }

        public void Err(Exception exception)
        {
            Controller.Log?.ReportException(exception);
        }

        public void Warn(string message)
        {
            Controller.Log?.Warning(message);
        }

        public void Info(string message)
        {
            Controller.Log?.Message(message);
        }

        #endregion
    }
}
