using System;
using AutoPriorities.Core;
using Verse;

namespace AutoPriorities.Utils
{
    internal static class ExceptionUtil
    {
        private static void LogAllInnerExceptions(this Exception? e)
        {
            if (e != null)
                Controller.Log.Error(e.Message);
            do
            {
                e = e?.InnerException;
                if (e != null)
                    Controller.Log.Error(e.Message);
            } while (e != null);
        }

        public static void LogStackTrace(this Exception? e)
        {
            Controller.Log.Error("Messages:");
            e.LogAllInnerExceptions();
            Controller.Log.Error("Stack trace:");
            Controller.Log.Error(e?.StackTrace);
        }
    }
}