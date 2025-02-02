using System.Collections.Generic;
using AutoPriorities.Wrappers;
using NSubstitute;
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
            pawn1.IsCapableOfWholeWorkType(workTypes[0]).Returns(true);
            pawn1.IsCapableOfWholeWorkType(workTypes[1]).Returns(true);
            // pawn1.IsCapableOfWholeWorkType(_workTypes[2]).Returns(true);
            pawn1.IsCapableOfWholeWorkType(workTypes[3]).Returns(true);
            pawn1.AverageOfRelevantSkillsFor(workTypes[0]).Returns(1.5f);
            pawn1.AverageOfRelevantSkillsFor(workTypes[1]).Returns(4);
            pawn1.AverageOfRelevantSkillsFor(workTypes[2]).Returns(3);
            pawn1.AverageOfRelevantSkillsFor(workTypes[3]).Returns(3);
            pawn1.MaxLearningRateFactor(workTypes[0]).Returns(1);
            pawn1.MaxLearningRateFactor(workTypes[1]).Returns(1);
            pawn1.MaxLearningRateFactor(workTypes[2]).Returns(1);
            pawn1.MaxLearningRateFactor(workTypes[3]).Returns(1);

            var pawn2 = Substitute.For<IPawnWrapper>();
            pawn2.ThingID.Returns("pawn2");
            pawn2.IsCapableOfWholeWorkType(workTypes[0]).Returns(true);
            pawn2.IsCapableOfWholeWorkType(workTypes[1]).Returns(true);
            pawn2.IsCapableOfWholeWorkType(workTypes[2]).Returns(true);
            pawn2.IsCapableOfWholeWorkType(workTypes[3]).Returns(true);
            pawn2.AverageOfRelevantSkillsFor(workTypes[0]).Returns(3);
            pawn2.AverageOfRelevantSkillsFor(workTypes[1]).Returns(2);
            pawn2.AverageOfRelevantSkillsFor(workTypes[2]).Returns(4);
            pawn2.AverageOfRelevantSkillsFor(workTypes[3]).Returns(2);
            pawn2.MaxLearningRateFactor(workTypes[0]).Returns(1);
            pawn2.MaxLearningRateFactor(workTypes[1]).Returns(1);
            pawn2.MaxLearningRateFactor(workTypes[2]).Returns(1);
            pawn2.MaxLearningRateFactor(workTypes[3]).Returns(1);

            var pawn3 = Substitute.For<IPawnWrapper>();
            pawn3.ThingID.Returns("pawn3");
            pawn3.IsCapableOfWholeWorkType(workTypes[0]).Returns(true);
            pawn3.IsCapableOfWholeWorkType(workTypes[1]).Returns(true);
            pawn3.IsCapableOfWholeWorkType(workTypes[2]).Returns(true);
            pawn3.IsCapableOfWholeWorkType(workTypes[3]).Returns(true);
            pawn3.AverageOfRelevantSkillsFor(workTypes[0]).Returns(1.3f);
            pawn3.AverageOfRelevantSkillsFor(workTypes[1]).Returns(1);
            pawn3.AverageOfRelevantSkillsFor(workTypes[2]).Returns(3);
            pawn3.AverageOfRelevantSkillsFor(workTypes[3]).Returns(1);
            pawn3.MaxLearningRateFactor(workTypes[0]).Returns(1);
            pawn3.MaxLearningRateFactor(workTypes[1]).Returns(1);
            pawn3.MaxLearningRateFactor(workTypes[2]).Returns(1);
            pawn3.MaxLearningRateFactor(workTypes[3]).Returns(1);

            var pawn4 = Substitute.For<IPawnWrapper>();
            pawn4.ThingID.Returns("pawn4");
            pawn4.IsCapableOfWholeWorkType(workTypes[0]).Returns(true);
            pawn4.IsCapableOfWholeWorkType(workTypes[1]).Returns(true);
            pawn4.IsCapableOfWholeWorkType(workTypes[2]).Returns(true);
            pawn4.IsCapableOfWholeWorkType(workTypes[3]).Returns(true);
            pawn4.AverageOfRelevantSkillsFor(workTypes[0]).Returns(1);
            pawn4.AverageOfRelevantSkillsFor(workTypes[1]).Returns(2);
            pawn4.AverageOfRelevantSkillsFor(workTypes[2]).Returns(6);
            pawn4.AverageOfRelevantSkillsFor(workTypes[3]).Returns(7);
            pawn4.MaxLearningRateFactor(workTypes[0]).Returns(1);
            pawn4.MaxLearningRateFactor(workTypes[1]).Returns(1);
            pawn4.MaxLearningRateFactor(workTypes[2]).Returns(1);
            pawn4.MaxLearningRateFactor(workTypes[3]).Returns(1);

            pawns = new List<IPawnWrapper> { pawn1, pawn2, pawn3, pawn4 };
        }

        private void WorkTypes()
        {
            var cook = Substitute.For<IWorkTypeWrapper>();
            cook.DefName.Returns("cook");
            cook.WorkTags.Returns(WorkTags.Cooking);
            cook.RelevantSkillsCount.Returns(1);
            cook.LabelShort.Returns("cook");

            var haul = Substitute.For<IWorkTypeWrapper>();
            haul.DefName.Returns("haul");
            haul.WorkTags.Returns(WorkTags.Hauling);
            haul.RelevantSkillsCount.Returns(0);
            haul.LabelShort.Returns("haul");

            var mine = Substitute.For<IWorkTypeWrapper>();
            mine.DefName.Returns("mine");
            mine.WorkTags.Returns(WorkTags.Mining);
            mine.RelevantSkillsCount.Returns(1);
            mine.LabelShort.Returns("mine");

            var craft = Substitute.For<IWorkTypeWrapper>();
            craft.DefName.Returns("crafting");
            craft.WorkTags.Returns(WorkTags.Crafting);
            craft.RelevantSkillsCount.Returns(1);
            craft.LabelShort.Returns("crafting");

            workTypes = new List<IWorkTypeWrapper> { cook, haul, mine, craft };
        }
    }
}
