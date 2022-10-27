using System;
using System.IO;
using System.Linq;
using System.Text;
using AutoFixture;
using AutoPriorities;
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
    public class PawnsDataSerializerSaveTests
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
        public void LoadSavedData_SaveState_LoadAndSave_IdenticalResult()
        {
            // arrange
            _retriever.PawnsInPlayerFactionInCurrentMap()
                .Returns(_fixture.CreateMany<IPawnWrapper>());

            _retriever.WorkTypeDefsInPriorityOrder()
                .Returns(
                    TestHelper.WorkTypes.Select(
                        x => _fixture.Build<WorkType>()
                            .With(y => y.DefName, x)
                            .Create()));
            var fileContents = File.ReadAllBytes(TestHelper.SavePath);
            _retriever.PawnsDataXml.Returns(fileContents);

            var expectedString = Encoding.UTF8.GetString(fileContents);
            var loaded = _serializer.LoadSavedData();

            var actualBytes = Array.Empty<byte>();
            _retriever.WhenForAnyArgs(x => x.PawnsDataXml = Array.Empty<byte>())
                .Do(x => actualBytes = x.Arg<byte[]>());

            // act
            _serializer.SaveData(
                new SaveDataRequest { ExcludedPawns = loaded!.ExcludedPawns, WorkTablesData = loaded.WorkTablesData });
            var actualString = Encoding.UTF8.GetString(actualBytes);

            // assert
            _logger.NoWarnReceived();
            actualString.Should()
                .Be(expectedString);
        }
    }
}
