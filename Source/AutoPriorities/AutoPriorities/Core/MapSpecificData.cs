using System;
using System.Collections.Generic;
using Verse;

namespace AutoPriorities.Core
{
    public class MapSpecificData : MapComponent, IMapSpecificData
    {
        private bool _forbidNonAdultsFromSelectedJobs = true;
        private bool _hasOpenedDialogOnce;
        private bool _ignoreDownedStatus;
        private bool _ignoreLearningRate;
        private bool _ignoreOppositionToWork;
        private bool _ignoreWorkSpeed;
        private List<string>? _importantWorkTypes = new() { "Firefighter", "Patient", "PatientBedRest", "BasicWorker" };
        private float _minimumSkillLevel = 3;
        private bool _runOnTimer;

        public MapSpecificData(Map map) : base(map)
        {
            var copy = Controller.AbandonedMapMapSpecificData;
            if (copy == null) return;

            Controller.AbandonedMapMapSpecificData = null;
            _ignoreLearningRate = copy._ignoreLearningRate;
            _ignoreOppositionToWork = copy._ignoreOppositionToWork;
            _ignoreDownedStatus = copy._ignoreDownedStatus;
            _ignoreWorkSpeed = copy._ignoreWorkSpeed;
            _forbidNonAdultsFromSelectedJobs = copy._forbidNonAdultsFromSelectedJobs;
            _hasOpenedDialogOnce = copy._hasOpenedDialogOnce;
            _importantWorkTypes = copy._importantWorkTypes;
            _minimumSkillLevel = copy._minimumSkillLevel;
            _runOnTimer = copy._runOnTimer;
            PawnsDataXml = copy.PawnsDataXml;
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

        public bool IgnoreDownedStatus
        {
            get => _ignoreDownedStatus;
            set => _ignoreDownedStatus = value;
        }

        public bool IgnoreWorkSpeed
        {
            get => _ignoreWorkSpeed;
            set => _ignoreWorkSpeed = value;
        }

        public bool ForbidNonAdultsFromSelectedJobs
        {
            get => _forbidNonAdultsFromSelectedJobs;
            set => _forbidNonAdultsFromSelectedJobs = value;
        }

        public bool RunOnTimer
        {
            get => _runOnTimer;
            set => _runOnTimer = value;
        }

        public bool HasOpenedDialogOnce
        {
            get => _hasOpenedDialogOnce;
            set => _hasOpenedDialogOnce = value;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref _importantWorkTypes, "AutoPriorities_ImportantWorkTypes", LookMode.Value);
            Scribe_Values.Look(ref _minimumSkillLevel, "AutoPriorities_MinimumSkillLevel");
            Scribe_Values.Look(ref _ignoreLearningRate, "AutoPriorities_IgnoreLearningRate");
            Scribe_Values.Look(ref _ignoreOppositionToWork, "AutoPriorities_IgnoreOppositionToWork");
            Scribe_Values.Look(ref _ignoreDownedStatus, "AutoPriorities_IgnoreDownedStatus");
            Scribe_Values.Look(ref _ignoreWorkSpeed, "AutoPriorities_IgnoreWorkSpeed");
            Scribe_Values.Look(ref _forbidNonAdultsFromSelectedJobs, "AutoPriorities_ForbidNonAdultsFromSelectedJobs", true);
            Scribe_Values.Look(ref _runOnTimer, "runOncePerDay");
            Scribe_Values.Look(ref _hasOpenedDialogOnce, "AutoPriorities_HasOpenedDialogOnce");

            var dataStr = Convert.ToBase64String(PawnsDataXml ?? Array.Empty<byte>());
            Scribe_Values.Look(ref dataStr, "AutoPriorities_PawnsDataXml");
            PawnsDataXml = string.IsNullOrEmpty(dataStr) ? null : Convert.FromBase64String(dataStr);
        }

        public static MapSpecificData? GetForCurrentMap()
        {
            var currentMap = Find.CurrentMap;
            if (currentMap == null)
            {
                Controller.logger?.Err("Called GetMapComponent on a null map");
                return null;
            }

            if (Controller.DebugLogs)
                Controller.logger?.Info("MapSpecificData retrieved");

            return currentMap.GetComponent<MapSpecificData>();
        }
    }
}
