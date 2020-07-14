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
            using var stream = new FileStream(FullPath, FileMode.Create);
            new XmlSerializer(typeof(Ser)).Serialize(stream, Ser.Serialized(state));
        }

        public static List<(int, Dictionary<WorkTypeDef, IPercent>)> LoadState()
        {
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

        private static WorkTypeDef? StringToDef(string name)
        {
            foreach (var workType in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder)
            {
                if (workType.defName == name)
                    return workType;
            }

            Controller.Log!.Message($"Work type {name} not found. Excluding {name} from the internal data structure.");
            return null;
        }

        public class Ser
        {
            public List<Tupl> data = new List<Tupl>();

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
            public Dic dict = new Dic();

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
            public List<StrPercent> percents = new List<StrPercent>();

            public Dictionary<WorkTypeDef, IPercent> Parsed() => percents
                .Select(x => x.Parsed())
                .Where(x => x.Item1 != null)
                .ToDictionary(x => x.Item1!, x => x.Item2);

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
            public string workType = "";
            public UnionPercent percent = new UnionPercent();

            public (WorkTypeDef?, IPercent) Parsed() => (StringToDef(workType), percent.Parsed());

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

            // Controller.PoolPercents.Acquire(new PercentPoolArgs{Value = workType._percent})
            public IPercent Parsed() => variant switch
            {
                Variant.Number => Controller.PoolNumbers.Acquire(new NumberPoolArgs {Count = number, Total = 0}),
                Variant.Percent => Controller.PoolPercents.Acquire(new PercentPoolArgs {Value = percent}),
                _ => throw new Exception()
            };

            public static UnionPercent Serialized(IPercent percent)
            {
                return percent switch
                {
                    Number n => new UnionPercent
                    {
                        variant = Variant.Number,
                        number = n.Count
                    },
                    Percent p => new UnionPercent
                    {
                        variant = Variant.Percent,
                        percent = p.Value
                    },
                    _ => throw new Exception(),
                };
            }
        }

        public enum Variant
        {
            Number,
            Percent
        }
    }
}