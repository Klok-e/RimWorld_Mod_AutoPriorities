using System;
using HugsLib.Utils;

namespace AutoPriorities.APLogger
{
    public class Logger : ILogger
    {
        private readonly ModLogger _controller;

        public Logger(ModLogger controller)
        {
            _controller = controller;
        }

        #region ILogger Members

        public void Err(string message)
        {
            _controller.Error(message);
        }

        public void Err(Exception exception)
        {
            _controller.ReportException(exception);
        }

        public void Warn(string message)
        {
            _controller.Warning(message);
        }

        public void Info(string message)
        {
            _controller.Message(message);
        }

        #endregion
    }
}
