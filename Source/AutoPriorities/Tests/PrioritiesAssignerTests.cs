using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoPriorities;
using AutoPriorities.APLogger;
using AutoPriorities.ImportantJobs;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.Percents;
using AutoPriorities.WorldInfoRetriever;
using AutoPriorities.Wrappers;
using NSubstitute;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests
{
    [TestFixture]
    public class PrioritiesAssignerTests
    {
        private PrioritiesAssigner _assigner = null!;
        private IFixture _fixture = null!;
        private IImportantJobsProvider _importantWorkTypesProvider = null!;
        private ILogger _logger = null!;
        private PawnsData _pawnsData = null!;
        private PawnWorktypeCreator _pw = null!;
        private IWorldInfoRetriever _retriever = null!;
        private IPawnsDataSerializer _serializer = null!;
        private IWorldInfoFacade _worldInfo = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _retriever = Substitute.For<IWorldInfoRetriever>();
            _worldInfo = new WorldInfoFacade(_retriever, _logger);
            _serializer = Substitute.For<IPawnsDataSerializer>();
            _fixture = FixtureBuilder.Create();
            _pw = PawnWorktypeCreator.Create();
            // AddMorePawnsToPw();
            _retriever.PawnsInPlayerFaction()
                      .Returns(_pw.pawns);
            _retriever.WorkTypeDefsInPriorityOrder()
                      .Returns(_pw.workTypes);
            AssignPawnsData();
            _importantWorkTypesProvider = Substitute.For<IImportantJobsProvider>();
            _importantWorkTypesProvider.ImportantWorkTypes()
                                       .Returns(new HashSet<IWorkTypeWrapper>());
            _assigner = new PrioritiesAssigner(_worldInfo, _pawnsData, _logger, _importantWorkTypesProvider);
        }

        [Test]
        public void AssignPriorities_Numbers()
        {
            // arrange

            // act
            _assigner.AssignPriorities();

            // assert
            _logger.NoWarnReceived();

            _pw.pawns[0]
               .Received()
               .workSettingsSetPriority(_pw.workTypes[1], 1);
            _pw.pawns[1]
               .Received()
               .workSettingsSetPriority(_pw.workTypes[0], 1);
            _pw.pawns[3]
               .Received()
               .workSettingsSetPriority(_pw.workTypes[2], 1);
            _pw.pawns[3]
               .Received()
               .workSettingsSetPriority(_pw.workTypes[3], 1);
        }

        private void AssignPawnsData()
        {
            var percents = new[]
            {
                TablePercent.Number(0, 1), TablePercent.Number(0, 1), TablePercent.Number(0, 1),
                TablePercent.Number(0, 1)
            };
            var workTypePercent = _pw.workTypes.Zip(percents, (x, y) => (x, y))
                                     .ToDictionary(k => k.x, v => v.y);
            var save = new SaveData
            {
                ExcludedPawns = new HashSet<ExcludedPawnEntry>
                {
                    new()
                    {
                        workDef = _pw.workTypes[1]
                                     .defName,
                        pawnThingId = _pw.pawns[1]
                                         .ThingID
                    }
                },
                WorkTablesData = new List<WorkTableEntry>
                {
                    new() {priority = 1, jobCount = 4, workTypes = workTypePercent}
                }
            };
            _serializer.LoadSavedData()
                       .Returns(save);

            _pawnsData = new PawnsDataBuilder(_serializer, _retriever, _logger).Build();
        }
    }
}
