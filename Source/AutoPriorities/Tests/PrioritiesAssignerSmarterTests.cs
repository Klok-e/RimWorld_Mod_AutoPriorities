using System.Collections.Generic;
using System.Linq;
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
    public class PrioritiesAssignerSmarterTests
    {
        [SetUp]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _retriever = Substitute.For<IWorldInfoRetriever>();
            _serializer = Substitute.For<IPawnsDataSerializer>();
            _pw = PawnWorktypeCreator.Create();
            // AddMorePawnsToPw();
            _retriever.GetAdultPawnsInPlayerFactionInCurrentMap().Returns(_pw.pawns);
            _retriever.GetWorkTypeDefsInPriorityOrder().Returns(_pw.workTypes);
            AssignPawnsData();
            _importantWorkTypesProvider = Substitute.For<IImportantJobsProvider>();
            _importantWorkTypesProvider.ImportantWorkTypes().Returns(new HashSet<IWorkTypeWrapper>());
            _assigner = new PrioritiesAssigner(_pawnsData, _logger, _importantWorkTypesProvider);
        }

        private PrioritiesAssigner _assigner = null!;
        private IImportantJobsProvider _importantWorkTypesProvider = null!;
        private ILogger _logger = null!;
        private PawnsData _pawnsData = null!;
        private PawnWorktypeCreator _pw = null!;
        private IWorldInfoRetriever _retriever = null!;
        private IPawnsDataSerializer _serializer = null!;

        [Test]
        public void AssignPrioritiesSmarter_Numbers()
        {
            // arrange
            _pawnsData.MinimumSkillLevel = 0;

            // act
            _assigner.AssignPrioritiesSmarter();

            // assert
            _logger.NoWarnReceived();

            _pw.pawns[0].Received().WorkSettingsSetPriority(_pw.workTypes[0], 2);
            _pw.pawns[1].Received().WorkSettingsSetPriority(_pw.workTypes[0], 1);
            _pw.pawns[2].Received().WorkSettingsSetPriority(_pw.workTypes[1], 1);
            _pw.pawns[3].Received().WorkSettingsSetPriority(_pw.workTypes[2], 1);
            _pw.pawns[3].Received().WorkSettingsSetPriority(_pw.workTypes[3], 1);
        }

        [Test]
        public void AssignPrioritiesSmarter_MinimumFitness2()
        {
            // arrange
            _pawnsData.MinimumSkillLevel = 2;

            // act
            _assigner.AssignPrioritiesSmarter();

            // assert
            _logger.NoWarnReceived();

            _pw.pawns[0].Received().WorkSettingsSetPriority(_pw.workTypes[0], 0);
            _pw.pawns[1].Received().WorkSettingsSetPriority(_pw.workTypes[0], 1);
            _pw.pawns[2].Received().WorkSettingsSetPriority(_pw.workTypes[1], 1);
            _pw.pawns[3].Received().WorkSettingsSetPriority(_pw.workTypes[2], 1);
            _pw.pawns[3].Received().WorkSettingsSetPriority(_pw.workTypes[3], 1);
        }

        private void AssignPawnsData()
        {
            var percents = new[] { TablePercent.Number(1), TablePercent.Number(1), TablePercent.Number(1), TablePercent.Number(1) };
            var workTypePercent = _pw.workTypes.Zip(percents, (x, y) => (x, y)).ToDictionary(k => k.x, v => v.y);
            var save = new SaveData
            {
                ExcludedPawns =
                    new HashSet<ExcludedPawnEntry> { new() { WorkDef = _pw.workTypes[1].DefName, PawnThingId = _pw.pawns[1].ThingID } },
                WorkTablesData = new List<WorkTableEntry>
                {
                    new() { Priority = 1, JobCount = 4, WorkTypes = workTypePercent },
                    new() { Priority = 2, JobCount = 4, WorkTypes = workTypePercent },
                },
            };
            _serializer.LoadSavedData().Returns(save);

            _pawnsData = new PawnsDataBuilder(_serializer, _retriever, _logger).Build();
        }
    }
}
