using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using AutoPriorities.Percents;
using AutoPriorities.Utils;
using RimWorld;
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

        public static void SaveState(
            (List<(int priority, Dictionary<WorkTypeDef, IPercent> workTypes)>, HashSet<(WorkTypeDef, Pawn)>) state)
        {
            using var stream = new FileStream(FullPath, FileMode.Create);
            new XmlSerializer(typeof(Ser)).Serialize(stream, Ser.Serialized(state));
        }

        public static (Func<List<(int, Dictionary<WorkTypeDef, IPercent> )>> percents,
            Func<HashSet<(WorkTypeDef, Pawn)>> excluded)
            GetStateLoaders()
        {
            try
            {
                if (File.Exists(FullPath))
                {
                    using var stream = new FileStream(FullPath, FileMode.OpenOrCreate);
                    var ser = (Ser) new XmlSerializer(typeof(Ser)).Deserialize(stream);
                    return (() => ser.ParsedData(), () => ser.ParsedExcluded());
                }
            }
            catch (Exception e)
            {
                Controller.Log!.Error("Error while deserializing state");
                e.LogStackTrace();
            }

            return (() => new List<(int, Dictionary<WorkTypeDef, IPercent>)>(),
                () => new HashSet<(WorkTypeDef, Pawn)>());
        }

        private static WorkTypeDef? StringToDef(string name)
        {
            var work = WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder.FirstOrDefault(w => w.defName == name);
            if (!(work is null))
                return work;

            Controller.Log!.Warning($"Work type {name} not found. Excluding {name} from the internal data structure.");
            return null;
        }

        private static Pawn? IdToPawn(string pawnId)
        {
            var res = Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
                .FirstOrDefault(p => p.ThingID == pawnId);
            if (!(res is null)) 
                return res;
            
            Controller.Log!.Warning($"pawn {pawnId} wasn't found while deserializing data, skipping...");
            return null;
        }

        public class Ser
        {
            public List<Tupl> data = new List<Tupl>();
            public List<WorktypePawn> excludedPawns = new List<WorktypePawn>();

            public List<(int, Dictionary<WorkTypeDef, IPercent>)> ParsedData() => data
                .Select(x => x.Parsed())
                .ToList();

            public HashSet<(WorkTypeDef, Pawn)> ParsedExcluded() => excludedPawns
                .Select(x => x.Parsed())
                .Where(p => p.Item1 != null && p.Item2 != null)
                .Select(p => (p.Item1!, p.Item2!))
                .ToHashSet();

            public static Ser Serialized(
                (List<(int, Dictionary<WorkTypeDef, IPercent> )> percents, HashSet<(WorkTypeDef, Pawn)> excluded) data)
            {
                return new Ser
                {
                    data = data.percents
                        .Select(Tupl.Serialized)
                        .ToList(),
                    excludedPawns = data.excluded
                        .Select(WorktypePawn.Serialized)
                        .ToList()
                };
            }
        }

        public class WorktypePawn
        {
            public string workType = "";
            public string pawnId = "";

            public (WorkTypeDef?, Pawn?) Parsed() => (StringToDef(workType), IdToPawn(pawnId));

            public static WorktypePawn Serialized((WorkTypeDef work, Pawn pawn) data)
            {
                return new WorktypePawn
                {
                    workType = data.work.defName,
                    pawnId = data.pawn.ThingID,
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