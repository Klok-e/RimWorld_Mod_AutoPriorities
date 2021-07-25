using System;
using AutoPriorities.APLogger;
using NSubstitute;

namespace Tests.Helpers
{
    public static class TestHelper
    {
        public static void NoWarnReceived(this ILogger logger)
        {
            logger.DidNotReceive()
                   .Err(Arg.Any<Exception>());
            logger.DidNotReceive()
                   .Err(Arg.Any<string>());
            logger.DidNotReceive()
                   .Warn(Arg.Any<string>());
        }
    }
}
