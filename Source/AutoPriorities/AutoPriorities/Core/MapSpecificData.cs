using System;
using System.Collections.Generic;
using System.Linq;
using AutoPriorities.PawnDataSerializer;
using AutoPriorities.Wrappers;
using Verse;

namespace AutoPriorities.Core
{
    public class MapSpecificData : MapComponent, IMapSpecificData
    {
        private List<ExcludedPawnSerializableEntry>? _excludedPawns;
        private bool _ignoreLearningRate;
        private bool _ignoreOppositionToWork;
        private bool _ignoreWorkSpeed;
        private List<string>? _importantWorkTypes = new() { "Firefighter", "Patient", "PatientBedRest", "BasicWorker" };
        private float _minimumSkillLevel;
        private bool _runOnTimer;

        public MapSpecificData(Map map) : base(map)
        {
        }

        public List<string>? ImportantWorkTypes
        {
            get => _importantWorkTypes;
            set => _importantWorkTypes = value;
        }

        public byte[]? PawnsDataXml { get; set; }

        public float MinimumSkillLevel
        {
            get => _minimumSkillLevel;
            set => _minimumSkillLevel = value;
        }

        public List<ExcludedPawnEntry> ExcludedPawns
        {
            get => (_excludedPawns ?? new List<ExcludedPawnSerializableEntry>()).Where(x => x.workTypeDef != null && x.pawn != null)
                .Select(
                    x => new ExcludedPawnEntry
                    {
                        WorkDef = new WorkTypeWrapper(x.workTypeDef ?? throw new InvalidOperationException()),
                        Pawn = new PawnWrapper(x.pawn ?? throw new InvalidOperationException()),
                    }
                )
                .ToList();
            set => _excludedPawns = value.Select(
                    x => new ExcludedPawnSerializableEntry
                        {
                            pawn = x.Pawn.GetPawnOrThrow(), workTypeDef = x.WorkDef.GetWorkTypeDefOrThrow(),
                        }
                )
                .ToList();
        }

        public bool IgnoreLearningRate
        {
            get => _ignoreLearningRate;
            set => _ignoreLearningRate = value;
        }

        public bool IgnoreOppositionToWork
        {
            get => _ignoreOppositionToWork;
            set => _ignoreOppositionToWork = value;
        }

        public bool IgnoreWorkSpeed
        {
            get => _ignoreWorkSpeed;
            set => _ignoreWorkSpeed = value;
        }

        public bool RunOnTimer
        {
            get => _runOnTimer;
            set => _runOnTimer = value;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref _importantWorkTypes, "AutoPriorities_ImportantWorkTypes", LookMode.Value);
            Scribe_Values.Look(ref _minimumSkillLevel, "AutoPriorities_MinimumSkillLevel");
            Scribe_Values.Look(ref _ignoreLearningRate, "AutoPriorities_IgnoreLearningRate");
            Scribe_Values.Look(ref _ignoreOppositionToWork, "AutoPriorities_IgnoreOppositionToWork");
            Scribe_Values.Look(ref _ignoreWorkSpeed, "AutoPriorities_IgnoreWorkSpeed");
            Scribe_Values.Look(ref _runOnTimer, "runOncePerDay");
            Scribe_Collections.Look(ref _excludedPawns, "AutoPriorities_ExcludedPawns", LookMode.Deep);

            var dataStr = Convert.ToBase64String(PawnsDataXml ?? Array.Empty<byte>());
            Scribe_Values.Look(ref dataStr, "AutoPriorities_PawnsDataXml");
            PawnsDataXml = string.IsNullOrEmpty(dataStr) ? null : Convert.FromBase64String(dataStr);
        }

        public static MapSpecificData? GetForCurrentMap()
        {
            var map = Find.CurrentMap;
            if (map == null)
            {
                Log.Error("Called GetMapComponent on a null map");
                return null;
            }

            if (map.GetComponent<MapSpecificData>() == null) map.components.Add(new MapSpecificData(map));

            return map.GetComponent<MapSpecificData>();
        }
    }
}
