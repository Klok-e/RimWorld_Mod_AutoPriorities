using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    public class PawnsDataSerializerSaveTests
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
        public void LoadSavedData_SaveState_LoadAndSave_IdenticalResult()
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

            var expectedString = Encoding.UTF8.GetString(fileContents);
            var loaded = _saveDataHandler.GetSavedData(_mapSpecificData, _worldSpecificData);

            var actualBytes = Array.Empty<byte>();
            _mapSpecificData.WhenForAnyArgs(x => x.PawnsDataXml = Array.Empty<byte>()).Do(x => actualBytes = x.Arg<byte[]>());

            // act
            _saveDataHandler.SaveData(
                new SaveDataRequest { ExcludedPawns = loaded!.ExcludedPawns, WorkTablesData = loaded.WorkTablesData },
                _mapSpecificData,
                _worldSpecificData
            );
            var actualString = Encoding.UTF8.GetString(actualBytes);

            // assert
            _logger.NoWarnReceived();
            actualString.Should().Be(expectedString);
        }
    }
}
