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
                 .Returns(3);
            pawn1.AverageOfRelevantSkillsFor(workTypes[1])
                 .Returns(4);
            pawn1.AverageOfRelevantSkillsFor(workTypes[2])
                 .Returns(3);
            pawn1.AverageOfRelevantSkillsFor(workTypes[3])
                 .Returns(3);

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
                 .Returns(5);
            pawn2.AverageOfRelevantSkillsFor(workTypes[1])
                 .Returns(2);
            pawn2.AverageOfRelevantSkillsFor(workTypes[2])
                 .Returns(4);
            pawn2.AverageOfRelevantSkillsFor(workTypes[3])
                 .Returns(2);

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
                 .Returns(2);
            pawn3.AverageOfRelevantSkillsFor(workTypes[1])
                 .Returns(1);
            pawn3.AverageOfRelevantSkillsFor(workTypes[2])
                 .Returns(3);
            pawn3.AverageOfRelevantSkillsFor(workTypes[3])
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

            pawns = new List<IPawnWrapper> {pawn1, pawn2, pawn3, pawn4};
        }

        private void WorkTypes()
        {
            var cook = new WorkType
            {
                defName = "cook", workTags = WorkTags.Cooking, relevantSkillsCount = 1, labelShort = "cook"
            };
            var haul = new WorkType
            {
                defName = "haul", workTags = WorkTags.Hauling, relevantSkillsCount = 0, labelShort = "haul"
            };
            var mine = new WorkType
            {
                defName = "mine", workTags = WorkTags.Mining, relevantSkillsCount = 1, labelShort = "mine"
            };
            var craft = new WorkType
            {
                defName = "crafting", workTags = WorkTags.Crafting, relevantSkillsCount = 1, labelShort = "crafting"
            };
            workTypes = new List<IWorkTypeWrapper> {cook, haul, mine, craft};
        }
    }
}
