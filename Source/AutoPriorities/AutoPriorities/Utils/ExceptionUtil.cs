using System;
using AutoPriorities.Core;
using Verse;

namespace AutoPriorities.Utils
{
    internal static class ExceptionUtil
    {
        public static void LogAllInnerExceptions(this Exception? e)
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
    }
}