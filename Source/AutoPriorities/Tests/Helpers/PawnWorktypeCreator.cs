using System.Collections.Generic;
using AutoPriorities.Wrappers;
using NSubstitute;
using Tests.MockImplementations;
using Verse;

namespace Tests.Helpers
{
    public class PawnWorktypeCreator
    {
        public List<IPawnWrapper> pawns = null!;
        public List<IWorkTypeWrapper> workTypes = null!;

        private PawnWorktypeCreator()
        {
        }

        public static PawnWorktypeCreator Create()
        {
            var creator = new PawnWorktypeCreator();
            creator.WorkTypes();
            creator.Pawns();
            return creator;
        }

        private void Pawns()
        {
            var pawn1 = Substitute.For<IPawnWrapper>();
            pawn1.ThingID.Returns("pawn1");
            pawn1.IsCapableOfWholeWorkType(workTypes[0])
                .Returns(true);
            pawn1.IsCapableOfWholeWorkType(workTypes[1])
                .Returns(true);
            // pawn1.IsCapableOfWholeWorkType(_workTypes[2]).Returns(true);
            pawn1.IsCapableOfWholeWorkType(workTypes[3])
                .Returns(true);
            pawn1.AverageOfRelevantSkillsFor(workTypes[0])
                .Returns(1.5);
            pawn1.AverageOfRelevantSkillsFor(workTypes[1])
                .Returns(4);
            pawn1.AverageOfRelevantSkillsFor(workTypes[2])
                .Returns(3);
            pawn1.AverageOfRelevantSkillsFor(workTypes[3])
                .Returns(3);
            pawn1.MaxLearningRateFactor(workTypes[0])
                .Returns(1);
            pawn1.MaxLearningRateFactor(workTypes[1])
                .Returns(1);
            pawn1.MaxLearningRateFactor(workTypes[2])
                .Returns(1);
            pawn1.MaxLearningRateFactor(workTypes[3])
                .Returns(1);

            var pawn2 = Substitute.For<IPawnWrapper>();
            pawn2.ThingID.Returns("pawn2");
            pawn2.IsCapableOfWholeWorkType(workTypes[0])
                .Returns(true);
            pawn2.IsCapableOfWholeWorkType(workTypes[1])
                .Returns(true);
            pawn2.IsCapableOfWholeWorkType(workTypes[2])
                .Returns(true);
            pawn2.IsCapableOfWholeWorkType(workTypes[3])
                .Returns(true);
            pawn2.AverageOfRelevantSkillsFor(workTypes[0])
                .Returns(3);
            pawn2.AverageOfRelevantSkillsFor(workTypes[1])
                .Returns(2);
            pawn2.AverageOfRelevantSkillsFor(workTypes[2])
                .Returns(4);
            pawn2.AverageOfRelevantSkillsFor(workTypes[3])
                .Returns(2);
            pawn2.MaxLearningRateFactor(workTypes[0])
                .Returns(1);
            pawn2.MaxLearningRateFactor(workTypes[1])
                .Returns(1);
            pawn2.MaxLearningRateFactor(workTypes[2])
                .Returns(1);
            pawn2.MaxLearningRateFactor(workTypes[3])
                .Returns(1);

            var pawn3 = Substitute.For<IPawnWrapper>();
            pawn3.ThingID.Returns("pawn3");
            pawn3.IsCapableOfWholeWorkType(workTypes[0])
                .Returns(true);
            pawn3.IsCapableOfWholeWorkType(workTypes[1])
                .Returns(true);
            pawn3.IsCapableOfWholeWorkType(workTypes[2])
                .Returns(true);
            pawn3.IsCapableOfWholeWorkType(workTypes[3])
                .Returns(true);
            pawn3.AverageOfRelevantSkillsFor(workTypes[0])
                .Returns(1.3);
            pawn3.AverageOfRelevantSkillsFor(workTypes[1])
                .Returns(1);
            pawn3.AverageOfRelevantSkillsFor(workTypes[2])
                .Returns(3);
            pawn3.AverageOfRelevantSkillsFor(workTypes[3])
                .Returns(1);
            pawn3.MaxLearningRateFactor(workTypes[0])
                .Returns(1);
            pawn3.MaxLearningRateFactor(workTypes[1])
                .Returns(1);
            pawn3.MaxLearningRateFactor(workTypes[2])
                .Returns(1);
            pawn3.MaxLearningRateFactor(workTypes[3])
                .Returns(1);

            var pawn4 = Substitute.For<IPawnWrapper>();
            pawn4.ThingID.Returns("pawn4");
            pawn4.IsCapableOfWholeWorkType(workTypes[0])
                .Returns(true);
            pawn4.IsCapableOfWholeWorkType(workTypes[1])
                .Returns(true);
            pawn4.IsCapableOfWholeWorkType(workTypes[2])
                .Returns(true);
            pawn4.IsCapableOfWholeWorkType(workTypes[3])
                .Returns(true);
            pawn4.AverageOfRelevantSkillsFor(workTypes[0])
                .Returns(1);
            pawn4.AverageOfRelevantSkillsFor(workTypes[1])
                .Returns(2);
            pawn4.AverageOfRelevantSkillsFor(workTypes[2])
                .Returns(6);
            pawn4.AverageOfRelevantSkillsFor(workTypes[3])
                .Returns(7);
            pawn4.MaxLearningRateFactor(workTypes[0])
                .Returns(1);
            pawn4.MaxLearningRateFactor(workTypes[1])
                .Returns(1);
            pawn4.MaxLearningRateFactor(workTypes[2])
                .Returns(1);
            pawn4.MaxLearningRateFactor(workTypes[3])
                .Returns(1);

            pawns = new List<IPawnWrapper> { pawn1, pawn2, pawn3, pawn4 };
        }

        private void WorkTypes()
        {
            var cook = new WorkType
            {
                DefName = "cook", WorkTags = WorkTags.Cooking, RelevantSkillsCount = 1, LabelShort = "cook"
            };
            var haul = new WorkType
            {
                DefName = "haul", WorkTags = WorkTags.Hauling, RelevantSkillsCount = 0, LabelShort = "haul"
            };
            var mine = new WorkType
            {
                DefName = "mine", WorkTags = WorkTags.Mining, RelevantSkillsCount = 1, LabelShort = "mine"
            };
            var craft = new WorkType
            {
                DefName = "crafting", WorkTags = WorkTags.Crafting, RelevantSkillsCount = 1, LabelShort = "crafting"
            };
            workTypes = new List<IWorkTypeWrapper> { cook, haul, mine, craft };
        }
    }
}
