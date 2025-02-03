using System;
using AutoPriorities.APLogger;
using AutoPriorities.WorldInfoRetriever;
using NSubstitute;

namespace Tests.Helpers
{
    public static class TestHelper
    {
        public const string SavePath = "TestData/SaveFile1.txt";

        public static readonly string[] WorkTypes =
        {
            "Firefighter", "Patient", "Doctor", "PatientBedRest", "BasicWorker", "Warden", "Handling", "Cooking", "Hunting", "Construction",
            "Growing", "Mining", "PlantCutting", "Smithing", "Tailoring", "Art", "Crafting", "Hauling", "Cleaning", "Research",
        };

        public static readonly string[] WorkTypesTruncated =
        {
            "Firefighter", "Patient", "Doctor", "PatientBedRest", "BasicWorker", "Warden", "Handling", "Cooking", "Hunting", "Construction",
            "Growing", "Mining", "PlantCutting", "Smithing", "Tailoring",
        };

        public static void NoWarnReceived(this ILogger logger)
        {
            logger.DidNotReceive().Err(Arg.Any<Exception>());
            logger.DidNotReceive().Err(Arg.Any<string>());
            logger.DidNotReceive().Warn(Arg.Any<string>());
        }

        public static IWorldInfoRetriever CreateWorldInfoRetrieverSubstitute()
        {
            var worldInfoRetriever = Substitute.For<IWorldInfoRetriever>();
            worldInfoRetriever.OptimizationPopulationSize().Returns(256);
            worldInfoRetriever.OptimizationFeasibleSolutionTimeoutSeconds().Returns(10);
            worldInfoRetriever.OptimizationImprovementSeconds().Returns(1);
            worldInfoRetriever.OptimizationCrossoverRate().Returns(0.2f);
            worldInfoRetriever.OptimizationMutationRate().Returns(0.9f);

            return worldInfoRetriever;
        }
    }
}
