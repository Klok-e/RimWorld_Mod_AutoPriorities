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
            _retriever = Substitute.For<IWorldInfoRetriever>();
            _serializer = Substitute.For<IPawnsDataSerializer>();
            _importantWorkTypesProvider = Substitute.For<IImportantJobsProvider>();

            _importantWorkTypesProvider.ImportantWorkTypes().Returns(new HashSet<IWorkTypeWrapper>());

            LoadPawnsDataFromFile();

            _assigner = new PrioritiesAssigner(_pawnsData, _logger, _importantWorkTypesProvider);
        }

        private PrioritiesAssigner _assigner = null!;
        private IImportantJobsProvider _importantWorkTypesProvider = null!;
        private ILogger _logger = null!;
        private PawnsData _pawnsData = null!;
        private IWorldInfoRetriever _retriever = null!;
        private IPawnsDataSerializer _serializer = null!;
        private List<IWorkTypeWrapper> _workTypes;
        private List<IPawnWrapper> _pawns;
        private List<WorkTableEntry>? _workTablesData;

        [Test]
        public void AssignPrioritiesSmarter_MinimumFitness2()
        {
            // arrange
            _pawnsData.MinimumSkillLevel = 2;

            // act
            _assigner.AssignPrioritiesSmarter();

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

            GetAssignedPriorities().Count(x => x.Priority == 1).Should().Be(5);
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

        private void LoadPawnsDataFromFile()
        {
            using var readStream = File.OpenRead("TestData/Case1/PrioritiesSmarterWorkTables.xml");
            var workTables = readStream.DeserializeXml<ArraySimpleData<WorkTablesSimpleData>>();

            using var readStream1 = File.OpenRead("TestData/Case1/PrioritiesSmarterWorkTypes.xml");
            var workTypes = readStream1.DeserializeXml<ArraySimpleData<WorkTypeSimpleData>>();

            using var readStream2 = File.OpenRead("TestData/Case1/PrioritiesSmarterAllPlayerPawns.xml");
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

            _retriever.GetAdultPawnsInPlayerFactionInCurrentMap().Returns(_pawns);
            _retriever.GetWorkTypeDefsInPriorityOrder().Returns(_workTypes);

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

            var save = new SaveData { WorkTablesData = _workTablesData ?? throw new InvalidOperationException() };
            _serializer.LoadSavedData().Returns(save);

            _pawnsData = new PawnsDataBuilder(_serializer, _retriever, _logger).Build();
        }
    }
}
