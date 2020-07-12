using AutoPriorities.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using AutoPriorities.Percents;
using Verse;

namespace AutoPriorities.Core
{
    [Obsolete]
    public static class PercentPerWorkTypeSaver
    {
        private const string Filename = "ModAutoPrioritiesSave.xml";

        public static readonly string SavePathFull;

        private static readonly XmlSerializer Writer;

        public static bool SaveFileExists => File.Exists(SavePathFull);

        static PercentPerWorkTypeSaver()
        {
            SavePathFull = UnityEngine.Application.persistentDataPath + Filename;

            try
            {
                Writer = new XmlSerializer(typeof(State));
            }
            catch (Exception e)
            {
                e.LogStackTrace();
                throw;
            }
        }

        public static void SaveStateLegacy(List<(int priority, Dictionary<WorkTypeDef, IPercent> workTypes)> state)
        {
            using var stream = new FileStream(SavePathFull, FileMode.Create);
            var list = new State
            {
                _intAndLists = new List<IntAndList>(state.Count)
            };
            foreach (var tuple in state)
            {
                var listOfWorks = new List<WorkTypeAndFloat>(tuple.workTypes.Count);
                foreach (var keyValue in tuple.workTypes)
                {
                    listOfWorks.Add(new WorkTypeAndFloat()
                    {
                        _percent = keyValue.Value.Value,
                        _workTypeDefName = keyValue.Key.defName,
                    });
                }

                list._intAndLists.Add(new IntAndList()
                {
                    _list = listOfWorks,
                    _priority = tuple.priority,
                });
            }

            Writer.Serialize(stream, list);
        }

        public static List<(int, Dictionary<WorkTypeDef, IPercent>)> LoadStateLegacy()
        {
            var output = new List<(int, Dictionary<WorkTypeDef, IPercent>)>();
            using (var stream = new FileStream(SavePathFull, FileMode.Open))
            {
                var des = (State) Writer.Deserialize(stream);

                foreach (var prior in des._intAndLists)
                {
                    var dict = new Dictionary<WorkTypeDef, IPercent>();
                    foreach (var workType in prior._list)
                    {
                        var def = StringToDef(workType._workTypeDefName);
                        if (def is null) continue;
                        dict.Add(def, Controller.PoolPercents.Acquire(new PercentPoolArgs {Value = workType._percent}));
                    }

                    output.Add((prior._priority, dict));
                }
            }

            return output;
        }

        public static WorkTypeDef? StringToDef(string name)
        {
            foreach (var workType in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder)
            {
                if (workType.defName == name)
                    return workType;
            }

            Log.Error($"name {name} not found");
            return null;
        }

        [XmlRoot]
        public class State
        {
            [XmlElement] public List<IntAndList> _intAndLists = new List<IntAndList>();
        }

        public class IntAndList
        {
            [XmlAttribute] public int _priority;
            [XmlElement] public List<WorkTypeAndFloat> _list = new List<WorkTypeAndFloat>();
        }

        public class WorkTypeAndFloat
        {
            [XmlAttribute] public string _workTypeDefName = "";
            [XmlAttribute] public double _percent;
        }
    }
}