using System;
using System.Collections.Generic;
using System.Linq;
using AutoPriorities.Percents;
using AutoPriorities.WorldInfoRetriever;
using AutoPriorities.Wrappers;

namespace AutoPriorities.Core
{
    public static class PercentTableSaver
    {
        public enum Variant
        {
            Number,
            Percent
        }

        public class Ser
        {
            public List<Tupl> data = new();
            public List<WorktypePawn> excludedPawns = new();

            public List<WorkTableEntry> ParsedData(IWorldInfoFacade serializer)
            {
                return data.Select(x => x.Parsed(serializer))
                    .ToList();
            }

            public HashSet<ExcludedPawnEntry> ParsedExcluded(IWorldInfoFacade serializer)
            {
                return excludedPawns.Select(x => x.Parsed(serializer))
                    .Where(p => p.Item1 != null && p.Item2 != null)
                    .Select(
                        p => new ExcludedPawnEntry
                        {
                            WorkDef = p.Item1!.DefName, PawnThingId = p.Item2!.ThingID
                        })
                    .ToHashSet();
            }

            public static Ser Serialized((List<WorkTableEntry> percents, HashSet<ExcludedPawnEntry> excluded) data)
            {
                return new Ser
                {
                    data = data.percents.Select(Tupl.Serialized)
                        .ToList(),
                    excludedPawns = data.excluded.Select(WorktypePawn.Serialized)
                        .ToList()
                };
            }
        }

        public class WorktypePawn
        {
            public string pawnId = "";
            public string workType = "";

            public (IWorkTypeWrapper?, IPawnWrapper?) Parsed(IWorldInfoFacade serializer)
            {
                return (serializer.StringToDef(workType), serializer.IdToPawn(pawnId));
            }

            public static WorktypePawn Serialized(ExcludedPawnEntry data)
            {
                return new WorktypePawn { workType = data.WorkDef, pawnId = data.PawnThingId };
            }
        }

        public class Tupl
        {
            public Dic dict = new();
            public int jobsMax;
            public int priority;

            public WorkTableEntry Parsed(IWorldInfoFacade serializer)
            {
                return new WorkTableEntry
                {
                    Priority = priority, JobCount = jobsMax, WorkTypes = dict.Parsed(serializer)
                };
            }

            public static Tupl Serialized(WorkTableEntry val)
            {
                return new Tupl
                {
                    priority = val.Priority.v, jobsMax = val.JobCount.v, dict = Dic.Serialized(val.WorkTypes)
                };
            }
        }

        public class Dic
        {
            public List<StrPercent> percents = new();

            public Dictionary<IWorkTypeWrapper, TablePercent> Parsed(IWorldInfoFacade serializer)
            {
                return percents.Select(x => x.Parsed(serializer))
                    .Where(x => x.Item1 != null)
                    .ToDictionary(x => x.Item1!, x => x.Item2);
            }

            public static Dic Serialized(Dictionary<IWorkTypeWrapper, TablePercent> dic)
            {
                return new Dic
                {
                    percents = dic.Select(kv => StrPercent.Serialized((kv.Key, kv.Value)))
                        .ToList()
                };
            }
        }

        public class StrPercent
        {
            public UnionPercent percent = new();
            public string workType = "";

            public (IWorkTypeWrapper?, TablePercent) Parsed(IWorldInfoFacade serializer)
            {
                return (serializer.StringToDef(workType), percent.Parsed());
            }

            public static StrPercent Serialized((IWorkTypeWrapper work, TablePercent percent) val)
            {
                return new StrPercent { workType = val.work.DefName, percent = UnionPercent.Serialized(val.percent) };
            }
        }

        public class UnionPercent
        {
            public int number;
            public double percent;
            public Variant variant;

            // Controller.PoolPercents.Acquire(new PercentPoolArgs{Value = workType._percent})
            public TablePercent Parsed()
            {
                return variant switch
                {
                    Variant.Number => TablePercent.Number(0, number),
                    Variant.Percent => TablePercent.Percent(percent),
                    _ => throw new Exception()
                };
            }

            public static UnionPercent Serialized(TablePercent percent)
            {
                return percent.Variant switch
                {
                    PercentVariant.Number => new UnionPercent
                    {
                        variant = Variant.Number, number = percent.NumberCount
                    },
                    PercentVariant.Percent => new UnionPercent
                    {
                        variant = Variant.Percent, percent = percent.PercentValue
                    },
                    _ => throw new Exception()
                };
            }
        }
    }
}
