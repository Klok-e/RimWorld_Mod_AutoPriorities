using System;
using AutoPriorities.Core;

namespace AutoPriorities.Utils
{
    internal static class ExceptionUtil
    {
        public static void LogStackTrace(this Exception? e)
        {
            Controller.Log!.Error("Messages:");
            e.LogAllInnerExceptions();
            Controller.Log.Error("Stack trace:");
            Controller.Log.Error(e?.StackTrace);
        }

        private static void LogAllInnerExceptions(this Exception? e)
        {
            if (e != null) Controller.Log!.Error(e.Message);
            do
            {
                e = e?.InnerException;
                if (e != null) Controller.Log!.Error(e.Message);
            } while (e != null);
        }
    }
}
