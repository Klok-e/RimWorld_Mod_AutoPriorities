using AutoPriorities.Wrappers;

namespace AutoPriorities
{
    public interface IWorkSpeedCalculator
    {
        float AverageWorkSpeed(IPawnWrapper pawnWrapper, IWorkTypeWrapper work);
    }
}
