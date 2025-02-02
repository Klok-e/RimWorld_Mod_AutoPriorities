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
            _importantWorkTypesProvider = Substitute.For<IImportantJobsProvider>();

            _importantWorkTypesProvider.ImportantWorkTypes().Returns(new HashSet<IWorkTypeWrapper>());

            LoadPawnsDataFromFile();

            _assigner = new PrioritiesAssigner(_pawnsData, _logger, _importantWorkTypesProvider, _worldInfoRetriever);
        }

        private PrioritiesAssigner _assigner = null!;
        private IImportantJobsProvider _importantWorkTypesProvider = null!;
        private ILogger _logger = null!;
        private PawnsData _pawnsData = null!;
        private IWorldInfoRetriever _worldInfoRetriever = null!;
        private IPawnsDataSerializer _serializer = null!;
        private List<IWorkTypeWrapper> _workTypes;
        private List<IPawnWrapper> _pawns;
        private List<WorkTableEntry>? _workTablesData;

        [Test]
        public void AssignPrioritiesSmarter_FromFile()
        {
            // arrange
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

            // Human241 - Firefighter - 1
            // Human241 - Patient - 2
            // Human241 - Doctor - 0
            // Human241 - PatientBedRest - 3
            // Human241 - HaulingUrgent - 1
            // Human241 - Childcare - 0
            // Human241 - BasicWorker - 3
            // Human241 - FinishingOff - 1
            // Human241 - Warden - 0
            // Human241 - Handling - 3
            // Human241 - Cooking - 0
            // Human241 - Hunting - 4
            // Human241 - Construction - 2
            // Human241 - Growing - 2
            // Human241 - Mining - 2
            // Human241 - PlantCutting - 2
            // Human241 - Smithing - 0
            // Human241 - Tailoring - 0
            // Human241 - Art - 0
            // Human241 - Crafting - 0
            // Human241 - Hauling - 4
            // Human241 - Cleaning - 4
            // Human241 - DarkStudy - 4
            // Human241 - Research - 4
            //
            // Human67650 - Firefighter - 1
            // Human67650 - Patient - 2
            // Human67650 - Doctor - 4
            // Human67650 - PatientBedRest - 3
            // Human67650 - HaulingUrgent - 1
            // Human67650 - Childcare - 3
            // Human67650 - BasicWorker - 3
            // Human67650 - FinishingOff - 1
            // Human67650 - Warden - 2
            // Human67650 - Handling - 0
            // Human67650 - Cooking - 0
            // Human67650 - Hunting - 0
            // Human67650 - Construction - 4
            // Human67650 - Growing - 4
            // Human67650 - Mining - 4
            // Human67650 - PlantCutting - 4
            // Human67650 - Smithing - 0
            // Human67650 - Tailoring - 0
            // Human67650 - Art - 0
            // Human67650 - Crafting - 0
            // Human67650 - Hauling - 4
            // Human67650 - Cleaning - 4
            // Human67650 - DarkStudy - 0
            // Human67650 - Research - 0
            //
            // CreepJoiner103705 - Firefighter - 1
            // CreepJoiner103705 - Patient - 2
            // CreepJoiner103705 - Doctor - 1
            // CreepJoiner103705 - PatientBedRest - 3
            // CreepJoiner103705 - HaulingUrgent - 1
            // CreepJoiner103705 - Childcare - 0
            // CreepJoiner103705 - BasicWorker - 3
            // CreepJoiner103705 - FinishingOff - 1
            // CreepJoiner103705 - Warden - 0
            // CreepJoiner103705 - Handling - 0
            // CreepJoiner103705 - Cooking - 0
            // CreepJoiner103705 - Hunting - 0
            // CreepJoiner103705 - Construction - 0
            // CreepJoiner103705 - Growing - 0
            // CreepJoiner103705 - Mining - 0
            // CreepJoiner103705 - PlantCutting - 0
            // CreepJoiner103705 - Smithing - 0
            // CreepJoiner103705 - Tailoring - 0
            // CreepJoiner103705 - Art - 2
            // CreepJoiner103705 - Crafting - 0
            // CreepJoiner103705 - Hauling - 4
            // CreepJoiner103705 - Cleaning - 4
            // CreepJoiner103705 - DarkStudy - 2
            // CreepJoiner103705 - Research - 2
            //
            // Human116785 - Firefighter - 1
            // Human116785 - Patient - 2
            // Human116785 - Doctor - 0
            // Human116785 - PatientBedRest - 3
            // Human116785 - HaulingUrgent - 1
            // Human116785 - Childcare - 3
            // Human116785 - BasicWorker - 3
            // Human116785 - FinishingOff - 0
            // Human116785 - Warden - 2
            // Human116785 - Handling - 0
            // Human116785 - Cooking - 0
            // Human116785 - Hunting - 4
            // Human116785 - Construction - 0
            // Human116785 - Growing - 0
            // Human116785 - Mining - 0
            // Human116785 - PlantCutting - 0
            // Human116785 - Smithing - 0
            // Human116785 - Tailoring - 0
            // Human116785 - Art - 0
            // Human116785 - Crafting - 0
            // Human116785 - Hauling - 4
            // Human116785 - Cleaning - 4
            // Human116785 - DarkStudy - 4
            // Human116785 - Research - 4

            GetAssignedPriorities().Count(x => x.Priority == 1).Should().Be(12);
            GetAssignedPriorities().Count(x => x.Priority == 2).Should().Be(15);
            GetAssignedPriorities().Count(x => x.Priority == 3).Should().Be(11);
            GetAssignedPriorities().Count(x => x.Priority == 4).Should().Be(17);
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

            var save = new SaveData { WorkTablesData = _workTablesData ?? throw new InvalidOperationException() };
            _serializer.LoadSavedData().Returns(save);

            _pawnsData = new PawnsDataBuilder(_serializer, _worldInfoRetriever, _logger).Build();
        }
    }
}
