using System;
using System.Collections.Generic;
using Verse;

namespace AutoPriorities.Core
{
    public class MapSpecificData : MapComponent, IMapSpecificData
    {
        private bool _ignoreLearningRate;
        private bool _ignoreOppositionToWork;
        private List<string>? _importantWorkTypes = new() { "Firefighter", "Patient", "PatientBedRest", "BasicWorker" };
        private float _minimumSkillLevel;

        public MapSpecificData(Map map)
            : base(map)
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

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref _importantWorkTypes, "AutoPriorities_ImportantWorkTypes", LookMode.Value);
            Scribe_Values.Look(ref _minimumSkillLevel, "AutoPriorities_MinimumSkillLevel");
            Scribe_Values.Look(ref _ignoreLearningRate, "AutoPriorities_IgnoreLearningRate");
            Scribe_Values.Look(ref _ignoreOppositionToWork, "AutoPriorities_IgnoreOppositionToWork");

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
