using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoPriorities;
using AutoPriorities.APLogger;
using AutoPriorities.ImportantJobs;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.SerializableSimpleData;
using AutoPriorities.Utils;
using AutoPriorities.WorldInfoRetriever;
using AutoPriorities.Wrappers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests
{
    [TestFixture]
    public class PrioritiesAssignerFromFileTests
    {
        [SetUp]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _worldInfoRetriever = TestHelper.CreateWorldInfoRetrieverSubstitute();
            _serializer = Substitute.For<IPawnsDataSerializer>();
            _workSpeedCalculator = Substitute.For<IWorkSpeedCalculator>();
            _importantWorkTypesProvider = Substitute.For<IImportantJobsProvider>();

            _importantWorkTypesProvider.ImportantWorkTypes().Returns(new HashSet<IWorkTypeWrapper>());

            _assigner = new PrioritiesAssigner(_pawnsData, _logger, _importantWorkTypesProvider, _worldInfoRetriever);
        }

        private PrioritiesAssigner _assigner = null!;
        private IImportantJobsProvider _importantWorkTypesProvider = null!;
        private ILogger _logger = null!;
        private PawnsData _pawnsData = null!;
        private IWorldInfoRetriever _worldInfoRetriever = null!;
        private IPawnsDataSerializer _serializer = null!;
        private List<IWorkTypeWrapper> _workTypes = null!;
        private List<IPawnWrapper> _pawns = null!;
        private List<WorkTableEntry>? _workTablesData;
        private IWorkSpeedCalculator _workSpeedCalculator = null!;

        [Test]
        public void AssignPrioritiesSmarter_FromFile()
        {
            // arrange
            LoadPawnsDataFromFile(1);
            _pawnsData.MinimumSkillLevel = 3;

            // act
            _assigner.AssignPrioritiesByOptimization(_pawnsData);

            // assert
            _logger.NoWarnReceived();

            // string.Join("\n\n", _pawns.Select(
            //     pawn => string.Join(
            //         "\n",
            //         pawn.ReceivedCalls()
            //             .Where(x => x.GetMethodInfo().Name == "WorkSettingsSetPriority")
            //             .Select(
            //                 x =>
            //                     $"{((IPawnWrapper)x.Target()).ThingID} - {((IWorkTypeWrapper)x.GetArguments()[0]).DefName} - {(x.GetArguments()[1])}")
            //             .ToList())))

            GetAssignedPriorities().Count(x => x.Priority == 0).Should().Be(41);
            GetAssignedPriorities().Count(x => x.Priority == 1).Should().Be(12);
            GetAssignedPriorities().Count(x => x.Priority == 2).Should().Be(15);
            GetAssignedPriorities().Count(x => x.Priority == 3).Should().Be(11);
            GetAssignedPriorities().Count(x => x.Priority == 4).Should().Be(17);
        }

        [Test]
        public void AssignPrioritiesSmarter_FromFileBig()
        {
            // arrange
            LoadPawnsDataFromFile(2);
            _pawnsData.MinimumSkillLevel = 3;

            // act
            _assigner.AssignPrioritiesByOptimization(_pawnsData);

            // assert
            _logger.NoWarnReceived();

            // string.Join("\n\n", _pawns.Select(
            //     pawn => string.Join(
            //         "\n",
            //         pawn.ReceivedCalls()
            //             .Where(x => x.GetMethodInfo().Name == "WorkSettingsSetPriority")
            //             .Select(
            //                 x =>
            //                     $"{((IPawnWrapper)x.Target()).ThingID} - {((IWorkTypeWrapper)x.GetArguments()[0]).DefName} - {(x.GetArguments()[1])}")
            //             .ToList())))

            GetAssignedPriorities().Count(x => x.Priority == 0).Should().Be(179);
            GetAssignedPriorities().Count(x => x.Priority == 1).Should().Be(63);
            GetAssignedPriorities().Count(x => x.Priority == 2).Should().Be(96);
            GetAssignedPriorities().Count(x => x.Priority == 3).Should().Be(97);
            GetAssignedPriorities().Count(x => x.Priority == 4).Should().Be(183);
        }

        [Test]
        public void AssignPrioritiesSmarter_FromFileLowMaxJobsPerPawn()
        {
            // arrange
            LoadPawnsDataFromFile(3);
            _pawnsData.MinimumSkillLevel = 3;

            // act
            _assigner.AssignPrioritiesByOptimization(_pawnsData);

            // assert
            _logger.NoWarnReceived();

            // string.Join("\n\n", _pawns.Select(
            //     pawn => string.Join(
            //         "\n",
            //         pawn.ReceivedCalls()
            //             .Where(x => x.GetMethodInfo().Name == "WorkSettingsSetPriority")
            //             .Select(
            //                 x =>
            //                     $"{((IPawnWrapper)x.Target()).ThingID} - {((IWorkTypeWrapper)x.GetArguments()[0]).DefName} - {(x.GetArguments()[1])}")
            //             .ToList())))

            GetMaxJobsForPawn().First(x => x.Priority == 2).Count.Should().Be(4);
        }

        [Test]
        public void AssignPrioritiesSmarter_FromFileJobSpread()
        {
            // arrange
            LoadPawnsDataFromFile(4);

            _pawnsData.MinimumSkillLevel = 3;

            _worldInfoRetriever.OptimizationJobsPerPawnWeight().Returns(10f);
            _worldInfoRetriever.OptimizationImprovementSeconds().Returns(4f);

            // act
            _assigner.AssignPrioritiesByOptimization(_pawnsData);

            // assert
            _logger.NoWarnReceived();

            var priorityGroupsCountMaxPerAllPawns = GetMaxJobsForPawn();

            priorityGroupsCountMaxPerAllPawns.First(x => x.Priority == 2).Count.Should().Be(7);
        }

        private List<(int Priority, int Count)> GetMaxJobsForPawn()
        {
            return GetAssignedPriorities()
                .GroupBy(x => x.Priority)
                .Select(x => (x.Key, x.GroupBy(y => y.Pawn).Select(y => y.Count()).Max()))
                .ToList();
        }

        private IEnumerable<(IPawnWrapper Pawn, IWorkTypeWrapper WorkType, int Priority)> GetAssignedPriorities()
        {
            return _pawns.SelectMany(
                pawn => pawn.ReceivedCalls()
                    .Where(x => x.GetMethodInfo().Name == nameof(IPawnWrapper.WorkSettingsSetPriority))
                    .Select(
                        x => (Pawn: (IPawnWrapper)x.Target(), WorkType: (IWorkTypeWrapper)x.GetArguments()[0]!,
                            Priority: (int)(x.GetArguments()[1] ?? throw new InvalidOperationException()))
                    )
                    .ToList()
            );
        }

        private void LoadPawnsDataFromFile(int testCase)
        {
            using var readStream = File.OpenRead($"TestData/Case{testCase}/PrioritiesSmarterWorkTables.xml");
            var workTables = readStream.DeserializeXml<ArraySimpleData<WorkTablesSimpleData>>();

            using var readStream1 = File.OpenRead($"TestData/Case{testCase}/PrioritiesSmarterWorkTypes.xml");
            var workTypes = readStream1.DeserializeXml<ArraySimpleData<WorkTypeSimpleData>>();

            using var readStream2 = File.OpenRead($"TestData/Case{testCase}/PrioritiesSmarterAllPlayerPawns.xml");
            var pawns = readStream2.DeserializeXml<ArraySimpleData<PawnSimpleData>>();

            _workTypes = workTypes.array?.Select(
                                 x =>
                                 {
                                     var workTypeWrapper = Substitute.For<IWorkTypeWrapper>();
                                     workTypeWrapper.WorkTags.Returns(x.workTags);
                                     workTypeWrapper.DefName.Returns(x.defName);
                                     workTypeWrapper.LabelShort.Returns(x.labelShort);
                                     workTypeWrapper.RelevantSkillsCount.Returns(x.relevantSkillsCount);

                                     return workTypeWrapper;
                                 }
                             )
                             .ToList()
                         ?? throw new InvalidOperationException();

            _pawns = pawns.array?.Select(
                             x =>
                             {
                                 var pawnWrapper = Substitute.For<IPawnWrapper>();
                                 pawnWrapper.NameFullColored.Returns(x.nameFullColored);
                                 pawnWrapper.ThingID.Returns(x.thingID);
                                 pawnWrapper.LabelNoCount.Returns(x.labelNoCount);
                                 foreach (var pawnWorkTypeData in x.pawnWorkTypeData)
                                 {
                                     var workTypeWrapper = _workTypes.First(y => y.DefName == pawnWorkTypeData.workTypeDefName);

                                     pawnWrapper.IsCapableOfWholeWorkType(workTypeWrapper)
                                         .Returns(pawnWorkTypeData.isCapableOfWholeWorkType);
                                     pawnWrapper.IsOpposedToWorkType(workTypeWrapper).Returns(pawnWorkTypeData.isOpposedToWorkType);
                                     pawnWrapper.AverageOfRelevantSkillsFor(workTypeWrapper)
                                         .Returns(pawnWorkTypeData.averageOfRelevantSkillsFor);
                                     pawnWrapper.MaxLearningRateFactor(workTypeWrapper).Returns(pawnWorkTypeData.maxLearningRateFactor);
                                 }

                                 return pawnWrapper;
                             }
                         )
                         .ToList()
                     ?? throw new InvalidOperationException();

            _worldInfoRetriever.GetAdultPawnsInPlayerFactionInCurrentMap().Returns(_pawns);
            _worldInfoRetriever.GetWorkTypeDefsInPriorityOrder().Returns(_workTypes);

            _workTablesData = workTables.array?.Select(
                    x =>
                    {
                        return new WorkTableEntry
                        {
                            Priority = x.priority,
                            JobCount = x.jobCount,
                            WorkTypes = x.workTypes?.Select(
                                                y =>
                                                {
                                                    return new
                                                        {
                                                            Key = _workTypes.First(w => w.DefName == y.key?.defName), Value = y.value,
                                                        };
                                                }
                                            )
                                            .ToDictionary(y => y.Key, y => y.Value)
                                        ?? throw new InvalidOperationException(),
                        };
                    }
                )
                .ToList();

            var save = new SaveData { WorkTablesData = _workTablesData ?? throw new InvalidOperationException(), IgnoreWorkSpeed = true };
            _serializer.LoadSavedData().Returns(save);

            _pawnsData = new PawnsDataBuilder(_serializer, _worldInfoRetriever, _logger, _workSpeedCalculator).Build();
        }
    }
}
