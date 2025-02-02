using System.Collections.Generic;
using AutoPriorities.Wrappers;

namespace AutoPriorities.SerializableSimpleData
{
    public class PawnSimpleData
    {
        public string? labelNoCount;
        public string? nameFullColored;
        public List<PawnWorkTypeData> pawnWorkTypeData = new();

        public string? thingID;

        public PawnSimpleData()
        {
        }

        public PawnSimpleData(IPawnWrapper pawnWrapper)
        {
            thingID = pawnWrapper.ThingID;
            nameFullColored = pawnWrapper.NameFullColored;
            labelNoCount = pawnWrapper.NameFullColored;
        }
    }
}
