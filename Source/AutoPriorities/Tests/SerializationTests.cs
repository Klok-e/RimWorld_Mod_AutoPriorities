using System;
using AutoFixture;
using AutoPriorities.APLogger;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.WorldInfoRetriever;
using AutoPriorities.Wrappers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests
{
    [TestFixture]
    public class SerializationTests
    {
        private readonly IFixture _fixture;
        private readonly ILogger _logger;
        private readonly IWorldInfoRetriever _retriever;
        private readonly IWorldInfoFacade _worldInfo;
        private readonly string savePath = "TestData/SaveFile1.xml";
        private readonly IPawnsDataSerializer _serializer;

        public SerializationTests()
        {
            _logger = Substitute.For<ILogger>();
            _retriever = Substitute.For<IWorldInfoRetriever>();
            _worldInfo = new WorldInfoFacade(_retriever, _logger);
            _serializer = new PawnsDataSerializer(_logger, savePath, _worldInfo);
            _fixture = FixtureBuilder.Create();
        }

        [SetUp]
        public void SetUp()
        {
            _logger.DidNotReceive()
                   .Err(Arg.Any<Exception>());
            _logger.DidNotReceive()
                   .Err(Arg.Any<string>());
            _logger.DidNotReceive()
                   .Warn(Arg.Any<string>());
        }

        [Test]
        public void LoadFromFile()
        {
            // arrange
            _retriever.PawnsInPlayerFaction()
                      .Returns(_fixture.CreateMany<IPawnWrapper>());

            _retriever.WorkTypeDefsInPriorityOrder()
                      .Returns(_fixture.CreateMany<IWorkTypeWrapper>());

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
        }
    }
}
