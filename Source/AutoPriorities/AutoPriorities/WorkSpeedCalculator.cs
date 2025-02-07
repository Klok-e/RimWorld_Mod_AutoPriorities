using System.Collections.Generic;
using System.Linq;
using AutoPriorities.APLogger;
using AutoPriorities.WorldInfoRetriever;
using AutoPriorities.Wrappers;
using RimWorld;
using Verse;

namespace AutoPriorities
{
    public class WorkSpeedCalculator : IWorkSpeedCalculator
    {
        private static readonly StatDef CookSpeed = DefDatabase<StatDef>.GetNamed("CookSpeed");

        private readonly ILogger _logger;
        private readonly IWorldInfoRetriever _worldInfoRetriever;

        public WorkSpeedCalculator(ILogger logger, IWorldInfoRetriever worldInfoRetriever)
        {
            _logger = logger;
            _worldInfoRetriever = worldInfoRetriever;
        }


        public float AverageWorkSpeed(IPawnWrapper pawnWrapper, IWorkTypeWrapper work)
        {
            var pawn = pawnWrapper.GetPawnOrThrow();

            var statDef = work.DefName switch
            {
                "Mining" => StatDefOf.MiningSpeed,
                "PlantCutting" => StatDefOf.PlantHarvestYield,
                "Growing" => StatDefOf.PlantWorkSpeed,
                "Research" => StatDefOf.PlantWorkSpeed,
                "Construction" => StatDefOf.ConstructSuccessChance,
                "Handling" => StatDefOf.AnimalGatherSpeed,
                "Cooking" => CookSpeed,
                "Doctor" => StatDefOf.MedicalTendSpeed,
                "Social" => StatDefOf.NegotiationAbility,
                "Hauling" => StatDefOf.MoveSpeed,
                "Cleaning" => StatDefOf.MoveSpeed,
                "Hunting" => StatDefOf.HuntingStealth,
                _ => StatDefOf.GeneralLaborSpeed,
            };

            if (_worldInfoRetriever.DebugLogs())
            {
                var postProcessStats = string.Join(", ", (statDef.postProcessStatFactors ?? new List<StatDef>()).Select(x => x.defName));
                _logger.Info(
                    $"Work type: {work.DefName}; stat: {statDef.defName}; "
                    + $"value: {pawn.GetStatValue(statDef)}; post process stats: {postProcessStats}"
                );
            }

            return pawn.GetStatValue(statDef);
        }
    }
}
