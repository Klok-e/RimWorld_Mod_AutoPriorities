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
        private IFixture _fixture = null!;
        private ILogger _logger = null!;
        private IWorldInfoRetriever _retriever = null!;
        private IPawnsDataSerializer _serializer = null!;
        private MemoryStream _stream = null!;
        private IWorldInfoFacade _worldInfo = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _retriever = Substitute.For<IWorldInfoRetriever>();
            _worldInfo = new WorldInfoFacade(_retriever, _logger);
            _stream = new MemoryStream();
            _serializer = new PawnsDataSerializer(_logger, TestHelper.SavePath, _worldInfo,
                new MemoryStreamProvider(_stream));
            _fixture = FixtureBuilder.Create();
        }

        [Test]
        public void LoadSavedData_SaveState_LoadAndSave_IdenticalResult()
        {
            // arrange
            _retriever.PawnsInPlayerFaction()
                      .Returns(_fixture.CreateMany<IPawnWrapper>());

            _retriever.WorkTypeDefsInPriorityOrder()
                      .Returns(TestHelper.WorkTypes.Select(x => _fixture.Build<WorkType>()
                                                                        .With(y => y.defName, x)
                                                                        .Create()));
            var fileContents = File.ReadAllBytes(TestHelper.SavePath);
            _stream.Write(fileContents, 0, fileContents.Length);
            _stream.Position = 0;
            var loaded = _serializer.LoadSavedData();
            var expectedString = Encoding.UTF8.GetString(_stream.ToArray());
            _stream.SetLength(0);

            // act
            _serializer.SaveData(new SaveDataRequest
            {
                ExcludedPawns = loaded.ExcludedPawns,
                WorkTablesData = loaded.WorkTablesData
                                       .Select(t => (t.priority, t.maxJobs ?? TestHelper.WorkTypes.Length, t.workTypes))
                                       .ToList()
            });
            var actualString = Encoding.UTF8.GetString(_stream.ToArray());

            // assert
            _logger.NoWarnReceived();
            actualString.Should()
                        .Be(expectedString);
        }
    }
}
