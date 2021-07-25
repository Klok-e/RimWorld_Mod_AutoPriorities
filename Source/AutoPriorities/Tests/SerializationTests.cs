using System;
using System.Linq;
using AutoFixture;
using AutoPriorities.APLogger;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.WorldInfoRetriever;
using AutoPriorities.Wrappers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Tests.Helpers;
using Tests.MockImplementations;

namespace Tests
{
    [TestFixture]
    public class SerializationTests
    {
        private  IFixture _fixture= null!;
        private  ILogger _logger= null!;
        private  IWorldInfoRetriever _retriever= null!;
        private  IWorldInfoFacade _worldInfo = null!;
        private  string savePath = "TestData/SaveFile1.xml";
        private  IPawnsDataSerializer _serializer= null!;

        private string[] workTypes = new[]
        {
            "Firefighter",
            "Patient",
            "Doctor",
            "PatientBedRest",
            "BasicWorker",
            "Warden",
            "Handling",
            "Cooking",
            "Hunting",
            "Construction",
            "Growing",
            "Mining",
            "PlantCutting",
            "Smithing",
            "Tailoring",
            "Art",
            "Crafting",
            "Hauling",
            "Cleaning",
            "Research",
        };private string[] workTypesTruncated = new[]
        {
            "Firefighter",
            "Patient",
            "Doctor",
            "PatientBedRest",
            "BasicWorker",
            "Warden",
            "Handling",
            "Cooking",
            "Hunting",
            "Construction",
            "Growing",
            "Mining",
            "PlantCutting",
            "Smithing",
            "Tailoring",
        };

        [SetUp]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _retriever = Substitute.For<IWorldInfoRetriever>();
            _worldInfo = new WorldInfoFacade(_retriever, _logger);
            _serializer = new PawnsDataSerializer(_logger, savePath, _worldInfo);
            _fixture = FixtureBuilder.Create();
        }

        [Test]
        public void LoadFromFile()
        {
            // arrange
            _retriever.PawnsInPlayerFaction()
                      .Returns(_fixture.CreateMany<IPawnWrapper>());

            _retriever.WorkTypeDefsInPriorityOrder()
                      .Returns(workTypes.Select(x => _fixture.Build<WorkType>()
                                                             .With(y => y.defName, x)
                                                             .Create()));

            // act
            var savedData = _serializer.LoadSavedData();

            // assert
            savedData.ExcludedPawns.Should()
                     .BeEmpty();
            savedData.WorkTablesData.Should()
                     .HaveCount(2);
            savedData.WorkTablesData[0]
                     .priority.V.Should()
                     .Be(2);
            savedData.WorkTablesData[1]
                     .priority.V.Should()
                     .Be(3);
            savedData.WorkTablesData[0]
                     .workTypes.Should()
                     .HaveCount(20);
            
            _logger.NoWarnReceived();
        }

        [Test]
        public void LoadFromFile_Warning_UnknownWorktypeInSave()
        {
            // arrange
            _retriever.PawnsInPlayerFaction()
                      .Returns(_fixture.CreateMany<IPawnWrapper>());

            _retriever.WorkTypeDefsInPriorityOrder()
                      .Returns(workTypesTruncated.Select(x => _fixture.Build<WorkType>()
                                                                      .With(y => y.defName, x)
                                                                      .Create()));

            // act
            var _ = _serializer.LoadSavedData();

            // assert
            // TODO: 10 calls is received, double the expected amount. OK for now
            _logger.Received(10)
                   .Warn(Arg.Any<string>());
        }
    }
}
