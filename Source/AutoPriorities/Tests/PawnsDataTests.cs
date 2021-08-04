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
using Tests.MockImplementations;

namespace Tests
{
    [TestFixture]
    public class PawnsDataBuilderTests
    {
        private ILogger _logger = null!;
        private PawnsDataBuilder _pawnsData = null!;
        private PawnWorktypeCreator _pw = null!;
        private IWorldInfoRetriever _retriever = null!;
        private IPawnsDataSerializer _serializer = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _retriever = Substitute.For<IWorldInfoRetriever>();
            _serializer = Substitute.For<IPawnsDataSerializer>();
            _pawnsData = new PawnsDataBuilder(_serializer, _retriever, _logger);
            FixtureBuilder.Create();
            _pw = PawnWorktypeCreator.Create();
            _retriever.PawnsInPlayerFaction()
                      .Returns(_pw.pawns);
            _retriever.WorkTypeDefsInPriorityOrder()
                      .Returns(_pw.workTypes);
        }

        [Test]
        public void Build()
        {
            // arrange

            var percents = new[]
            {
                TablePercent.Percent(0.2), TablePercent.Percent(0.2), TablePercent.Percent(0.2),
                TablePercent.Number(0, 2)
            };
            var workTypePercent = _pw.workTypes.Zip(percents, (x, y) => (x, y))
                                     .ToDictionary(k => k.x, v => v.y);
            var save = new SaveData
            {
                ExcludedPawns = new HashSet<ExcludedPawnEntry>
                {
                    new()
                    {
                        WorkDef = _pw.workTypes[1]
                                     .DefName,
                        PawnThingId = _pw.pawns[1]
                                         .ThingID
                    }
                },
                WorkTablesData = new List<WorkTableEntry>
                {
                    new() { Priority = 1, JobCount = 4, WorkTypes = workTypePercent }
                }
            };
            _serializer.LoadSavedData()
                       .Returns(save);

            // act
            var pawnData = _pawnsData.Build();

            // assert
            _logger.NoWarnReceived();

            pawnData.SortedPawnFitnessForEveryWork[_pw.workTypes[0]]
                    .Select(x => x.pawn)
                    .Should()
                    .Equal(_pw.pawns[1], _pw.pawns[0], _pw.pawns[2], _pw.pawns[3]);
            pawnData.SortedPawnFitnessForEveryWork[_pw.workTypes[1]]
                    .Select(x => x.pawn)
                    .Should()
                    .Equal(_pw.pawns[0], _pw.pawns[3], _pw.pawns[2]);
            pawnData.SortedPawnFitnessForEveryWork[_pw.workTypes[2]]
                    .Select(x => x.pawn)
                    .Should()
                    .Equal(_pw.pawns[3], _pw.pawns[1], _pw.pawns[2]);
            pawnData.SortedPawnFitnessForEveryWork[_pw.workTypes[3]]
                    .Select(x => x.pawn)
                    .Should()
                    .Equal(_pw.pawns[3], _pw.pawns[0], _pw.pawns[1], _pw.pawns[2]);
        }

        [Test]
        public void Build_WorkTypeNotInSaveButInGame()
        {
            // arrange

            var percents = new[]
            {
                TablePercent.Percent(0.2), TablePercent.Percent(0.2), TablePercent.Percent(0.2),
                TablePercent.Number(0, 2)
            };
            var unknownWorkType = new WorkType { DefName = "unknown" };
            _pw.workTypes.Add(unknownWorkType);
            var workTypePercent = _pw.workTypes.Zip(percents, (x, y) => (x, y))
                                     .ToDictionary(k => k.x, v => v.y);
            var save = new SaveData
            {
                ExcludedPawns = new HashSet<ExcludedPawnEntry>
                {
                    new()
                    {
                        WorkDef = _pw.workTypes[1]
                                     .DefName,
                        PawnThingId = _pw.pawns[1]
                                         .ThingID
                    }
                },
                WorkTablesData = new List<WorkTableEntry>
                {
                    new() { Priority = 1, JobCount = 4, WorkTypes = workTypePercent }
                }
            };
            _serializer.LoadSavedData()
                       .Returns(save);

            // act
            var pd = _pawnsData.Build();

            // assert
            _logger.Received(1)
                   .Warn(Arg.Any<string>());

            pd.WorkTables.First()
              .WorkTypes.Should()
              .HaveCount(5);
        }
    }
}
