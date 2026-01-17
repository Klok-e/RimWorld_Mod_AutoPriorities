using System.Collections.Generic;
using System.Linq;
using AutoPriorities;
using AutoPriorities.APLogger;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.Percents;
using AutoPriorities.WorldInfoRetriever;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests
{
    [TestFixture]
    public class PawnsDataSeedFromWorkTabTests
    {
        [Test]
        public void SeedFromWorkTab_UsesAssignedPrioritiesAndJobCounts()
        {
            var logger = Substitute.For<ILogger>();
            var serializer = Substitute.For<IPawnsDataSerializer>();
            var retriever = Substitute.For<IWorldInfoRetriever>();
            var workSpeedCalculator = Substitute.For<IWorkSpeedCalculator>();

            var pawnsData = new PawnsData(serializer, retriever, logger, workSpeedCalculator);

            var creator = PawnWorktypeCreator.Create();
            var pawn1 = creator.pawns[0];
            var pawn2 = creator.pawns[1];
            var work1 = creator.workTypes[0];
            var work2 = creator.workTypes[1];

            pawn1.WorkSettingsGetPriority(work1).Returns(1);
            pawn1.WorkSettingsGetPriority(work2).Returns(1);
            pawn2.WorkSettingsGetPriority(work1).Returns(2);
            pawn2.WorkSettingsGetPriority(work2).Returns(1);

            pawnsData.WorkTables.Clear();
            pawnsData.WorkTypes.Add(work1);
            pawnsData.WorkTypes.Add(work2);
            pawnsData.CurrentMapPlayerPawns.AddRange(new[] { pawn1, pawn2 });

            pawnsData.SortedPawnFitnessForEveryWork[work1] =
                new List<PawnFitnessData>
                {
                    new()
                    {
                        Pawn = pawn1, SkillLevel = 5, IsOpposed = false, IsSkilledWorkType = true,
                    },
                    new()
                    {
                        Pawn = pawn2, SkillLevel = 5, IsOpposed = false, IsSkilledWorkType = true,
                    },
                };

            pawnsData.SortedPawnFitnessForEveryWork[work2] =
                new List<PawnFitnessData>
                {
                    new()
                    {
                        Pawn = pawn1, SkillLevel = 5, IsOpposed = false, IsSkilledWorkType = true,
                    },
                    new()
                    {
                        Pawn = pawn2, SkillLevel = 5, IsOpposed = false, IsSkilledWorkType = true,
                    },
                };

            var seeded = pawnsData.SeedFromWorkTab(true);

            seeded.Should().BeTrue();
            pawnsData.WorkTables.Should().HaveCount(2);

            var priority1 = pawnsData.WorkTables.Single(x => x.Priority.v == 1);
            priority1.WorkTypes[work1].variant.Should().Be(PercentVariant.Percent);
            priority1.WorkTypes[work1].percentValue.Should().BeApproximately(0.5d, 0.0001d);
            priority1.WorkTypes[work2].variant.Should().Be(PercentVariant.Percent);
            priority1.WorkTypes[work2].percentValue.Should().BeApproximately(1.0d, 0.0001d);
            priority1.JobCount.v.Should().Be(2);

            var priority2 = pawnsData.WorkTables.Single(x => x.Priority.v == 2);
            priority2.WorkTypes[work1].variant.Should().Be(PercentVariant.Percent);
            priority2.WorkTypes[work1].percentValue.Should().BeApproximately(0.5d, 0.0001d);
            priority2.WorkTypes[work2].variant.Should().Be(PercentVariant.Percent);
            priority2.WorkTypes[work2].percentValue.Should().BeApproximately(0.0d, 0.0001d);
            priority2.JobCount.v.Should().Be(1);
        }
    }
}
