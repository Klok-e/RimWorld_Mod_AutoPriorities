using System;
using Verse;

namespace AutoPriorities.Utils
{
    internal static class ExceptionUtil
    {
        public static void LogAllInnerExceptions(Exception e)
        {
            Log.Error(e.Message);
            do
            {
                e = e.InnerException ?? null;
                if(e != null)
                    Log.Error(e.Message);
            }
            while(e != null);
        }
    }
}
