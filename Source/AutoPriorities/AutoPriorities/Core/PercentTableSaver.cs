using System.Collections.Generic;
using System.IO;
using AutoPriorities.Percents;
using Newtonsoft.Json;
using Verse;

namespace AutoPriorities.Core
{
    public static class PercentTableSaver
    {
        private const string Filename = "ModAutoPrioritiesSaveNEW.json";
        private static readonly string FullPath;

        static PercentTableSaver()
        {
            FullPath = Path.Combine(UnityEngine.Application.persistentDataPath, Filename);
        }

        public static void SaveState(List<(int priority, Dictionary<WorkTypeDef, IPercent> workTypes)> state)
        {
            // delete legacy file if exists
            if (PercentPerWorkTypeSaver.SaveFileExists)
                File.Delete(PercentPerWorkTypeSaver.SavePathFull);

            var contents = JsonConvert.SerializeObject(state);
            File.WriteAllText(FullPath, contents);
        }

        public static List<(int, Dictionary<WorkTypeDef, IPercent>)> LoadState()
        {
            // load legacy file if exists
            if (PercentPerWorkTypeSaver.SaveFileExists)
                return PercentPerWorkTypeSaver.LoadStateLegacy();

            var text = File.ReadAllText(FullPath);
            return JsonConvert.DeserializeObject<List<(int, Dictionary<WorkTypeDef, IPercent>)>>(text);
        }
    }
}