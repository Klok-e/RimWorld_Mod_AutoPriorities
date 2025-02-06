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
            Percent,
            PercentRemaining,
        }

        public class Ser
        {
            public List<Tupl> data = new();

            // serialized from file only now for backward compatibility, TODO: delete sometimes in the future
            public List<WorktypePawn> excludedPawns = new();

            public List<WorkTableEntry> ParsedData(IWorldInfoFacade serializer)
            {
                return data.Select(x => x.Parsed(serializer)).ToList();
            }

            public HashSet<ExcludedPawnEntry> ParsedExcluded(IWorldInfoFacade serializer)
            {
                return excludedPawns.Select(x => x.Parsed(serializer))
                    .Where(p => p is { Item1: not null, Item2: not null })
                    .Select(p => new ExcludedPawnEntry { WorkDef = p.Item1!, Pawn = p.Item2! })
                    .ToHashSet();
            }

            public static Ser Serialized(List<WorkTableEntry> percents)
            {
                return new Ser { data = percents.Select(Tupl.Serialized).ToList() };
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
        }

        public class Tupl
        {
            public Dic dict = new();
            public int jobsMax;
            public int priority;

            public WorkTableEntry Parsed(IWorldInfoFacade serializer)
            {
                return new WorkTableEntry { Priority = priority, JobCount = jobsMax, WorkTypes = dict.Parsed(serializer) };
            }

            public static Tupl Serialized(WorkTableEntry val)
            {
                return new Tupl { priority = val.Priority.v, jobsMax = val.JobCount.v, dict = Dic.Serialized(val.WorkTypes) };
            }
        }

        public class Dic
        {
            public List<StrPercent> percents = new();

            public Dictionary<IWorkTypeWrapper, TablePercent> Parsed(IWorldInfoFacade serializer)
            {
                return percents.Select(x => x.Parsed(serializer)).Where(x => x.Item1 != null).ToDictionary(x => x.Item1!, x => x.Item2);
            }

            public static Dic Serialized(Dictionary<IWorkTypeWrapper, TablePercent> dic)
            {
                return new Dic { percents = dic.Select(kv => StrPercent.Serialized((kv.Key, kv.Value))).ToList() };
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
                    Variant.Number => TablePercent.Number(number),
                    Variant.Percent => TablePercent.Percent(percent),
                    Variant.PercentRemaining => TablePercent.PercentRemaining(),
                    _ => throw new Exception(),
                };
            }

            public static UnionPercent Serialized(TablePercent percent)
            {
                return percent.variant switch
                {
                    PercentVariant.Number => new UnionPercent { variant = Variant.Number, number = percent.NumberCount },
                    PercentVariant.Percent => new UnionPercent { variant = Variant.Percent, percent = percent.PercentValue },
                    PercentVariant.PercentRemaining => new UnionPercent { variant = Variant.PercentRemaining },
                    _ => throw new Exception(),
                };
            }
        }
    }
}
