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

        private void AddMorePawnsToPw()
        {
            var pawn5 = Substitute.For<IPawnWrapper>();
            pawn5.ThingID.Returns("pawn1");
            pawn5.IsCapableOfWholeWorkType(_pw.workTypes[0])
                 .Returns(true);
            pawn5.IsCapableOfWholeWorkType(_pw.workTypes[1])
                 .Returns(true);
            // pawn1.IsCapableOfWholeWorkType(_pw._workTypes[2]).Returns(true);
            pawn5.IsCapableOfWholeWorkType(_pw.workTypes[3])
                 .Returns(true);
            pawn5.AverageOfRelevantSkillsFor(_pw.workTypes[0])
                 .Returns(3);
            pawn5.AverageOfRelevantSkillsFor(_pw.workTypes[1])
                 .Returns(4);
            pawn5.AverageOfRelevantSkillsFor(_pw.workTypes[2])
                 .Returns(3);
            pawn5.AverageOfRelevantSkillsFor(_pw.workTypes[3])
                 .Returns(3);

            var pawn6 = Substitute.For<IPawnWrapper>();
            pawn6.ThingID.Returns("pawn2");
            pawn6.IsCapableOfWholeWorkType(_pw.workTypes[0])
                 .Returns(true);
            pawn6.IsCapableOfWholeWorkType(_pw.workTypes[1])
                 .Returns(true);
            pawn6.IsCapableOfWholeWorkType(_pw.workTypes[2])
                 .Returns(true);
            pawn6.IsCapableOfWholeWorkType(_pw.workTypes[3])
                 .Returns(true);
            pawn6.AverageOfRelevantSkillsFor(_pw.workTypes[0])
                 .Returns(5);
            pawn6.AverageOfRelevantSkillsFor(_pw.workTypes[1])
                 .Returns(2);
            pawn6.AverageOfRelevantSkillsFor(_pw.workTypes[2])
                 .Returns(4);
            pawn6.AverageOfRelevantSkillsFor(_pw.workTypes[3])
                 .Returns(2);

            var pawn7 = Substitute.For<IPawnWrapper>();
            pawn7.ThingID.Returns("pawn3");
            pawn7.IsCapableOfWholeWorkType(_pw.workTypes[0])
                 .Returns(true);
            pawn7.IsCapableOfWholeWorkType(_pw.workTypes[1])
                 .Returns(true);
            pawn7.IsCapableOfWholeWorkType(_pw.workTypes[2])
                 .Returns(true);
            pawn7.IsCapableOfWholeWorkType(_pw.workTypes[3])
                 .Returns(true);
            pawn7.AverageOfRelevantSkillsFor(_pw.workTypes[0])
                 .Returns(2);
            pawn7.AverageOfRelevantSkillsFor(_pw.workTypes[1])
                 .Returns(1);
            pawn7.AverageOfRelevantSkillsFor(_pw.workTypes[2])
                 .Returns(3);
            pawn7.AverageOfRelevantSkillsFor(_pw.workTypes[3])
                 .Returns(1);

            var pawn8 = Substitute.For<IPawnWrapper>();
            pawn8.ThingID.Returns("pawn4");
            pawn8.IsCapableOfWholeWorkType(_pw.workTypes[0])
                 .Returns(true);
            pawn8.IsCapableOfWholeWorkType(_pw.workTypes[1])
                 .Returns(true);
            pawn8.IsCapableOfWholeWorkType(_pw.workTypes[2])
                 .Returns(true);
            pawn8.IsCapableOfWholeWorkType(_pw.workTypes[3])
                 .Returns(true);
            pawn8.AverageOfRelevantSkillsFor(_pw.workTypes[0])
                 .Returns(1);
            pawn8.AverageOfRelevantSkillsFor(_pw.workTypes[1])
                 .Returns(2);
            pawn8.AverageOfRelevantSkillsFor(_pw.workTypes[2])
                 .Returns(6);
            pawn8.AverageOfRelevantSkillsFor(_pw.workTypes[3])
                 .Returns(7);

            _pw.pawns.AddRange(new[] {pawn5, pawn6, pawn7});
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
            _serializer.LoadSavedData()
                       .Returns(new SaveData
                       {
                           ExcludedPawns =
                               new HashSet<(IWorkTypeWrapper, IPawnWrapper)> {(_pw.workTypes[1], _pw.pawns[1])},
                           WorkTablesData = new List<WorkTableEntry>
                           {
                               new() {priority = 1, jobCount = 4, workTypes = workTypePercent}
                           }
                       });

            _pawnsData = new PawnsDataBuilder(_serializer, _retriever, _logger).Build();
        }
    }
}
