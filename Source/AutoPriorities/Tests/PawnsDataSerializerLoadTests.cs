using System.IO;
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
    public class PawnsDataSerializerLoadTests
    {
        [SetUp]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _retriever = Substitute.For<IWorldInfoRetriever>();
            _worldInfo = new WorldInfoFacade(_retriever, _logger);
            _serializer = new MapSpecificDataPawnsDataSerializer(
                _retriever,
                new PawnDataStringSerializer(_logger, _worldInfo));
            _fixture = FixtureBuilder.Create();
        }

        private IFixture _fixture = null!;
        private ILogger _logger = null!;
        private IWorldInfoRetriever _retriever = null!;
        private IPawnsDataSerializer _serializer = null!;
        private IWorldInfoFacade _worldInfo = null!;

        [Test]
        public void LoadFromFile()
        {
            // arrange
            _retriever.PawnsInPlayerFaction()
                .Returns(_fixture.CreateMany<IPawnWrapper>());

            _retriever.WorkTypeDefsInPriorityOrder()
                .Returns(
                    TestHelper.WorkTypes.Select(
                        x => _fixture.Build<WorkType>()
                            .With(y => y.DefName, x)
                            .Create()));
            var fileContents = File.ReadAllBytes(TestHelper.SavePath);
            _retriever.PawnsDataXml.Returns(fileContents);

            // act
            var savedData = _serializer.LoadSavedData();

            // assert
            savedData.Should()
                .NotBeNull();
            savedData!.ExcludedPawns.Should()
                .BeEmpty();
            savedData.WorkTablesData.Should()
                .HaveCount(2);
            savedData.WorkTablesData[0]
                .Priority.v.Should()
                .Be(2);
            savedData.WorkTablesData[1]
                .Priority.v.Should()
                .Be(3);
            savedData.WorkTablesData[0]
                .WorkTypes.Should()
                .HaveCount(20);

            _logger.NoWarnReceived();
        }

        [Test]
        public void LoadSavedData_Warning_UnknownWorktypeInSave()
        {
            // arrange
            _retriever.PawnsInPlayerFaction()
                .Returns(_fixture.CreateMany<IPawnWrapper>());

            _retriever.WorkTypeDefsInPriorityOrder()
                .Returns(
                    TestHelper.WorkTypesTruncated.Select(
                        x => _fixture.Build<WorkType>()
                            .With(y => y.DefName, x)
                            .Create()));
            var fileContents = File.ReadAllBytes(TestHelper.SavePath);
            _retriever.PawnsDataXml.Returns(fileContents);

            // act
            var _ = _serializer.LoadSavedData();

            // assert
            _logger.Received(10)
                .Warn(Arg.Any<string>());
        }
    }
}
