using System;
using System.Reflection;
using AutoPriorities.APLogger;
using NSubstitute;

namespace Tests.Helpers
{
    public static class TestHelper
    {
        public const string SavePath = "TestData/SaveFile1.xml";

        public static readonly string[] WorkTypes =
        {
            "Firefighter", "Patient", "Doctor", "PatientBedRest", "BasicWorker", "Warden", "Handling", "Cooking",
            "Hunting", "Construction", "Growing", "Mining", "PlantCutting", "Smithing", "Tailoring", "Art",
            "Crafting", "Hauling", "Cleaning", "Research"
        };

        public static readonly string[] WorkTypesTruncated =
        {
            "Firefighter", "Patient", "Doctor", "PatientBedRest", "BasicWorker", "Warden", "Handling", "Cooking",
            "Hunting", "Construction", "Growing", "Mining", "PlantCutting", "Smithing", "Tailoring"
        };

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
