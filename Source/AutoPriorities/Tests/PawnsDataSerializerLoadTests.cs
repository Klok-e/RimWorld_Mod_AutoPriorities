using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoFixture;
using AutoPriorities;
using AutoPriorities.APLogger;
using AutoPriorities.Core;
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
    public class PawnsDataSerializerLoadTests
    {
        [SetUp]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _retriever = Substitute.For<IWorldInfoRetriever>();
            _worldInfo = new WorldInfoFacade(_retriever, _logger);
            var pawnDataStringSerializer = new PawnDataStringSerializer(_logger, _worldInfo);
            _saveDataHandler = new SaveDataHandler(_logger, pawnDataStringSerializer);
            _fixture = FixtureBuilder.Create();

            _mapSpecificData = Substitute.For<IMapSpecificData>();
            _worldSpecificData = Substitute.For<IWorldSpecificData>();
        }

        private IFixture _fixture = null!;
        private ILogger _logger = null!;
        private IWorldInfoRetriever _retriever = null!;
        private IWorldInfoFacade _worldInfo = null!;
        private SaveDataHandler _saveDataHandler = null!;
        private IMapSpecificData _mapSpecificData = null!;
        private IWorldSpecificData _worldSpecificData = null!;

        [Test]
        public void LoadFromFile()
        {
            // arrange
            _retriever.GetAdultPawnsInPlayerFactionInCurrentMap().Returns(_fixture.CreateMany<IPawnWrapper>());

            _retriever.GetWorkTypeDefsInPriorityOrder()
                .Returns(
                    TestHelper.WorkTypes.Select(
                        x =>
                        {
                            var workTypeWrapper = _fixture.Create<IWorkTypeWrapper>();
                            workTypeWrapper.DefName.Returns(x);
                            return workTypeWrapper;
                        }
                    )
                );
            var fileContents = File.ReadAllBytes(TestHelper.SavePath);
            _mapSpecificData.PawnsDataXml.Returns(fileContents);
            _worldSpecificData.ExcludedPawns.Returns(new List<ExcludedPawnEntry>());

            // act
            var savedData = _saveDataHandler.GetSavedData(_mapSpecificData, _worldSpecificData);

            // assert
            savedData.Should().NotBeNull();
            savedData.ExcludedPawns.Should().BeEmpty();
            savedData.WorkTablesData.Should().HaveCount(2);
            savedData.WorkTablesData[0].Priority.v.Should().Be(2);
            savedData.WorkTablesData[1].Priority.v.Should().Be(3);
            savedData.WorkTablesData[0].WorkTypes.Should().HaveCount(20);

            _logger.NoWarnReceived();
        }

        [Test]
        public void LoadSavedData_Warning_UnknownWorktypeInSave()
        {
            // arrange
            _retriever.GetAdultPawnsInPlayerFactionInCurrentMap().Returns(_fixture.CreateMany<IPawnWrapper>());

            _retriever.GetWorkTypeDefsInPriorityOrder()
                .Returns(
                    TestHelper.WorkTypesTruncated.Select(
                        x =>
                        {
                            var workTypeWrapper = _fixture.Create<IWorkTypeWrapper>();
                            workTypeWrapper.DefName.Returns(x);
                            return workTypeWrapper;
                        }
                    )
                );
            var fileContents = File.ReadAllBytes(TestHelper.SavePath);
            _mapSpecificData.PawnsDataXml.Returns(fileContents);
            _worldSpecificData.ExcludedPawns.Returns(new List<ExcludedPawnEntry>());

            // act
            _ = _saveDataHandler.GetSavedData(_mapSpecificData, _worldSpecificData);

            // assert
            _logger.Received(10).Warn(Arg.Any<string>());
        }
    }
}
