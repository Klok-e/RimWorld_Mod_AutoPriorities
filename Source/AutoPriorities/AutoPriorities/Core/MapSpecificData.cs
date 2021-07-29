using System.Collections.Generic;
using Verse;

namespace AutoPriorities.Core
{
    public class MapSpecificData : MapComponent
    {
        public List<string> importantWorks =
            new List<string>() {"Firefighter", "Patient", "PatientBedRest", "BasicWorker"};

        public MapSpecificData(Map map)
            : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref importantWorks, "AutoPriorities_ImportantWorkTypes", LookMode.Value);
        }

        public static MapSpecificData? GetForCurrentMap()
        {
            var map = Find.CurrentMap;
            if (map == null)
            {
                Log.Error("Called GetMapComponent on a null map");
                return null;
            }

            if (map.GetComponent<MapSpecificData>() == null)
            {
                map.components.Add(new MapSpecificData(map));
            }

            return map.GetComponent<MapSpecificData>();
        }
    }
}
