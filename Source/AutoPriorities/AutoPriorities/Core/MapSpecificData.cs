using System;
using System.Collections.Generic;
using Verse;

namespace AutoPriorities.Core
{
    public class MapSpecificData : MapComponent
    {
        public List<string>? importantWorkTypes = new() { "Firefighter", "Patient", "PatientBedRest", "BasicWorker" };
        public byte[]? pawnsDataXml;

        public MapSpecificData(Map map)
            : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref importantWorkTypes, "AutoPriorities_ImportantWorkTypes", LookMode.Value);
            var dataStr = Convert.ToBase64String(pawnsDataXml ?? Array.Empty<byte>());
            Scribe_Values.Look(ref dataStr, "AutoPriorities_PawnsDataXml");
            pawnsDataXml = !string.IsNullOrEmpty(dataStr) ? Convert.FromBase64String(dataStr!) : null;
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
