using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using AutoPriorities.Percents;
using UnityEngine;
using UnityEngine.Serialization;
using Verse;

namespace AutoPriorities.Core
{
    public static class PercentTableSaver
    {
        private const string Filename = "ModAutoPrioritiesSaveNEW.xml";
        private static readonly string FullPath;

        static PercentTableSaver()
        {
            FullPath = Application.persistentDataPath + Filename;
        }

        public static void SaveState(List<(int priority, Dictionary<WorkTypeDef, IPercent> workTypes)> state)
        {
            // delete legacy file if exists
            if (PercentPerWorkTypeSaver.SaveFileExists)
                File.Delete(PercentPerWorkTypeSaver.SavePathFull);

            using var stream = new FileStream(FullPath, FileMode.Create);
            new XmlSerializer(typeof(Ser)).Serialize(stream, Ser.Serialized(state));
        }

        public static List<(int, Dictionary<WorkTypeDef, IPercent>)> LoadState()
        {
            // load legacy file if exists
            if (PercentPerWorkTypeSaver.SaveFileExists)
                return PercentPerWorkTypeSaver.LoadStateLegacy();

            if (File.Exists(FullPath))
            {
                using var stream = new FileStream(FullPath, FileMode.OpenOrCreate);
                var ser = (Ser) new XmlSerializer(typeof(Ser)).Deserialize(stream);
                return ser.Parsed();
            }
            else
            {
                return new List<(int, Dictionary<WorkTypeDef, IPercent>)>();
            }
        }

        private static WorkTypeDef StringToDef(string name)
        {
            foreach (var workType in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder)
            {
                if (workType.defName == name)
                    return workType;
            }

            Log.Error($"name {name} not found");
            return null;
        }

        public class Ser
        {
            public List<Tupl> data;

            public List<(int, Dictionary<WorkTypeDef, IPercent> )> Parsed() => data
                .Select(x => x.Parsed())
                .ToList();

            public static Ser Serialized(List<(int, Dictionary<WorkTypeDef, IPercent>)> data)
            {
                return new Ser
                {
                    data = data
                        .Select(Tupl.Serialized)
                        .ToList()
                };
            }
        }

        public class Tupl
        {
            public int priority;
            public Dic dict;

            public (int, Dictionary<WorkTypeDef, IPercent> ) Parsed() => (priority, dict.Parsed());

            public static Tupl Serialized((int, Dictionary<WorkTypeDef, IPercent>) val)
            {
                return new Tupl
                {
                    priority = val.Item1,
                    dict = Dic.Serialized(val.Item2)
                };
            }
        }

        public class Dic
        {
            public List<StrPercent> percents;

            public Dictionary<WorkTypeDef, IPercent> Parsed() => percents
                .Select(x => x.Parsed())
                .ToDictionary(x => x.Item1, x => x.Item2);

            public static Dic Serialized(Dictionary<WorkTypeDef, IPercent> dic)
            {
                return new Dic
                {
                    percents = dic
                        .Select(kv => StrPercent.Serialized((kv.Key, kv.Value)))
                        .ToList()
                };
            }
        }

        public class StrPercent
        {
            public string workType;
            public UnionPercent percent;

            public (WorkTypeDef, IPercent) Parsed() => (StringToDef(workType), percent.Parsed());

            public static StrPercent Serialized((WorkTypeDef work, IPercent percent) val)
            {
                return new StrPercent
                {
                    workType = val.work.defName,
                    percent = UnionPercent.Serialized(val.percent)
                };
            }
        }

        public class UnionPercent
        {
            public Variant variant;
            public int number;
            public double percent;

            public IPercent Parsed() => variant switch
            {
                Variant.Number => new Number(number, null),
                Variant.Percent => new Percent(percent),
                _ => throw new Exception()
            };

            public static UnionPercent Serialized(IPercent p)
            {
                return p.Variant switch
                {
                    Variant.Number => new UnionPercent
                    {
                        variant = Variant.Number,
                        number = ((Number) p).Count
                    },
                    Variant.Percent => new UnionPercent
                    {
                        variant = Variant.Percent,
                        percent = ((Percent) p).Value
                    },
                    _ => throw new Exception(),
                };
            }
        }
    }
}