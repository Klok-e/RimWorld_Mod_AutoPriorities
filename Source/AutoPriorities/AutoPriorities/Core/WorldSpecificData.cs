using System;
using System.Collections.Generic;
using System.Linq;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.Wrappers;
using RimWorld.Planet;
using Verse;

namespace AutoPriorities.Core
{
    public class WorldSpecificData : WorldComponent, IWorldSpecificData
    {
        private List<ExcludedPawnSerializableEntry>? _excludedPawns;


        public WorldSpecificData(World world) : base(world)
        {
        }

        public List<ExcludedPawnEntry> ExcludedPawns
        {
            get => (_excludedPawns ?? new List<ExcludedPawnSerializableEntry>()).Where(x => x.workTypeDef != null && x.pawn != null)
                .Select(
                    x =>
                        new ExcludedPawnEntry
                        {
                            WorkDef = new WorkTypeWrapper(x.workTypeDef ?? throw new InvalidOperationException()),
                            Pawn = new PawnWrapper(x.pawn ?? throw new InvalidOperationException()),
                        }
                )
                .ToList();
            set => _excludedPawns =
                value.Select(
                        x =>
                            new ExcludedPawnSerializableEntry
                            {
                                pawn = x.Pawn.GetPawnOrThrow(), workTypeDef = x.WorkDef.GetWorkTypeDefOrThrow(),
                            }
                    )
                    .ToList();
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref _excludedPawns, "AutoPriorities_ExcludedPawns", LookMode.Deep);
        }

        public static WorldSpecificData? GetForCurrentWorld()
        {
            var world = Find.World;
            if (world == null)
            {
                Log.Error("Called GetMapComponent on a null map");
                return null;
            }

            if (world.GetComponent<WorldSpecificData>() == null) world.components.Add(new WorldSpecificData(world));

            return world.GetComponent<WorldSpecificData>();
        }
    }
}
