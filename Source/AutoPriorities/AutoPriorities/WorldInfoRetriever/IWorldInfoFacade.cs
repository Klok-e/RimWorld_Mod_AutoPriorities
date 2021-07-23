using AutoPriorities.Wrappers;

namespace AutoPriorities.WorldInfoRetriever
{
    public interface IWorldInfoFacade
    {
        IWorkTypeWrapper? StringToDef(string name);

        IPawnWrapper? IdToPawn(string pawnId);
    }
}
