using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoPriorities;
using AutoPriorities.APLogger;
using AutoPriorities.Core;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.PawnDataSerializer.StreamProviders;
using AutoPriorities.Percents;
using AutoPriorities.WorldInfoRetriever;
using AutoPriorities.Wrappers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Tests.Helpers;
using Tests.MockImplementations;
using Verse;

namespace Tests
{
    [TestFixture]
    public class PawnsDataBuilderTests
    {
        private IFixture _fixture = null!;
        private ILogger _logger = null!;
        private IWorldInfoRetriever _retriever = null!;
        private IPawnsDataSerializer _serializer = null!;
        private IWorldInfoFacade _worldInfo = null!;
        private PawnsDataBuilder _pawnsData = null!;
        private IWorkTypeWrapper[] _workTypes = null!;
        private IPawnWrapper[] _pawns = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _retriever = Substitute.For<IWorldInfoRetriever>();
            _worldInfo = new WorldInfoFacade(_retriever, _logger);
            _serializer = Substitute.For<IPawnsDataSerializer>();
            _pawnsData = new PawnsDataBuilder(_serializer, _retriever, _logger);
            _fixture = FixtureBuilder.Create();
            _workTypes = WorkTypes();
            _pawns = Pawns();
            _retriever.PawnsInPlayerFaction()
                      .Returns(_pawns);
            _retriever.WorkTypeDefsInPriorityOrder()
                      .Returns(_workTypes);
        }

        [Test]
        public void Build()
        {
            // arrange

            var percents = new IPercent[]
            {
                new Percent().Initialize(new PercentPoolArgs() {Value = 0.2}),
                new Percent().Initialize(new PercentPoolArgs() {Value = 0.2}),
                new Percent().Initialize(new PercentPoolArgs() {Value = 0.2}),
                new Percent().Initialize(new PercentPoolArgs() {Value = 0.2}),
            };
            var workTypePercent = _workTypes.Zip(percents, (x, y) => (x, y))
                                            .ToDictionary(k => k.x, v => v.y);
            _serializer.LoadSavedData()
                       .Returns(new SaveData
                       {
                           ExcludedPawns = new HashSet<(IWorkTypeWrapper, IPawnWrapper)> {(_workTypes[1], _pawns[1])},
                           WorkTablesData =
                               new List<(Priority priority, JobCount? maxJobs, Dictionary<IWorkTypeWrapper, IPercent>
                                   workTypes)>()
                               {
                                   (1, 4, workTypePercent)
                               }
                       });

            // act
            var pawnData = _pawnsData.Build();

            // assert
            _logger.NoWarnReceived();

            pawnData.SortedPawnFitnessForEveryWork[_workTypes[0]]
                    .Select(x => x.pawn)
                    .Should()
                    .Equal(_pawns[1], _pawns[0], _pawns[2], _pawns[3]);
            pawnData.SortedPawnFitnessForEveryWork[_workTypes[1]]
                    .Select(x => x.pawn)
                    .Should()
                    .Equal(_pawns[0], _pawns[3], _pawns[2]);
            pawnData.SortedPawnFitnessForEveryWork[_workTypes[2]]
                    .Select(x => x.pawn)
                    .Should()
                    .Equal(_pawns[3], _pawns[1], _pawns[2]);
            pawnData.SortedPawnFitnessForEveryWork[_workTypes[3]]
                    .Select(x => x.pawn)
                    .Should()
                    .Equal(_pawns[3], _pawns[0], _pawns[1], _pawns[2]);
        }

        IWorkTypeWrapper[] WorkTypes()
        {
            var cook = new WorkType
            {
                defName = "cook",
                workTags = WorkTags.Cooking,
                relevantSkillsCount = 1,
                labelShort = "cook"
            };
            var haul = new WorkType
            {
                defName = "haul",
                workTags = WorkTags.Hauling,
                relevantSkillsCount = 0,
                labelShort = "haul"
            };
            var mine = new WorkType
            {
                defName = "mine",
                workTags = WorkTags.Mining,
                relevantSkillsCount = 1,
                labelShort = "mine"
            };
            var craft = new WorkType
            {
                defName = "crafting",
                workTags = WorkTags.Crafting,
                relevantSkillsCount = 1,
                labelShort = "crafting"
            };
            return new IWorkTypeWrapper[] {cook, haul, mine, craft};
        }

        IPawnWrapper[] Pawns()
        {
            var pawn1 = Substitute.For<IPawnWrapper>();
            pawn1.ThingID.Returns("pawn1");
            pawn1.AnimalOrWildMan()
                 .Returns(false);
            pawn1.IsCapableOfWholeWorkType(_workTypes[0])
                 .Returns(true);
            pawn1.IsCapableOfWholeWorkType(_workTypes[1])
                 .Returns(true);
            // pawn1.IsCapableOfWholeWorkType(_workTypes[2]).Returns(true);
            pawn1.IsCapableOfWholeWorkType(_workTypes[3])
                 .Returns(true);
            pawn1.AverageOfRelevantSkillsFor(_workTypes[0])
                 .Returns(3);
            pawn1.AverageOfRelevantSkillsFor(_workTypes[1])
                 .Returns(4);
            pawn1.AverageOfRelevantSkillsFor(_workTypes[2])
                 .Returns(3);
            pawn1.AverageOfRelevantSkillsFor(_workTypes[3])
                 .Returns(3);

            var pawn2 = Substitute.For<IPawnWrapper>();
            pawn2.ThingID.Returns("pawn2");
            pawn2.AnimalOrWildMan()
                 .Returns(false);
            pawn2.IsCapableOfWholeWorkType(_workTypes[0])
                 .Returns(true);
            pawn2.IsCapableOfWholeWorkType(_workTypes[1])
                 .Returns(true);
            pawn2.IsCapableOfWholeWorkType(_workTypes[2])
                 .Returns(true);
            pawn2.IsCapableOfWholeWorkType(_workTypes[3])
                 .Returns(true);
            pawn2.AverageOfRelevantSkillsFor(_workTypes[0])
                 .Returns(5);
            pawn2.AverageOfRelevantSkillsFor(_workTypes[1])
                 .Returns(2);
            pawn2.AverageOfRelevantSkillsFor(_workTypes[2])
                 .Returns(4);
            pawn2.AverageOfRelevantSkillsFor(_workTypes[3])
                 .Returns(2);

            var pawn3 = Substitute.For<IPawnWrapper>();
            pawn3.ThingID.Returns("pawn3");
            pawn3.AnimalOrWildMan()
                 .Returns(false);
            pawn3.IsCapableOfWholeWorkType(_workTypes[0])
                 .Returns(true);
            pawn3.IsCapableOfWholeWorkType(_workTypes[1])
                 .Returns(true);
            pawn3.IsCapableOfWholeWorkType(_workTypes[2])
                 .Returns(true);
            pawn3.IsCapableOfWholeWorkType(_workTypes[3])
                 .Returns(true);
            pawn3.AverageOfRelevantSkillsFor(_workTypes[0])
                 .Returns(2);
            pawn3.AverageOfRelevantSkillsFor(_workTypes[1])
                 .Returns(1);
            pawn3.AverageOfRelevantSkillsFor(_workTypes[2])
                 .Returns(3);
            pawn3.AverageOfRelevantSkillsFor(_workTypes[3])
                 .Returns(1);

            var pawn4 = Substitute.For<IPawnWrapper>();
            pawn4.ThingID.Returns("pawn4");
            pawn4.AnimalOrWildMan()
                 .Returns(false);
            pawn4.IsCapableOfWholeWorkType(_workTypes[0])
                 .Returns(true);
            pawn4.IsCapableOfWholeWorkType(_workTypes[1])
                 .Returns(true);
            pawn4.IsCapableOfWholeWorkType(_workTypes[2])
                 .Returns(true);
            pawn4.IsCapableOfWholeWorkType(_workTypes[3])
                 .Returns(true);
            pawn4.AverageOfRelevantSkillsFor(_workTypes[0])
                 .Returns(1);
            pawn4.AverageOfRelevantSkillsFor(_workTypes[1])
                 .Returns(2);
            pawn4.AverageOfRelevantSkillsFor(_workTypes[2])
                 .Returns(6);
            pawn4.AverageOfRelevantSkillsFor(_workTypes[3])
                 .Returns(7);

            return new[] {pawn1, pawn2, pawn3, pawn4};
        }
    }
}
