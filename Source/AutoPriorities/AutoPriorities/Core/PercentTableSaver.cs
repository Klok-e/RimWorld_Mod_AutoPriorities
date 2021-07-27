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
        public class Ser
        {
            public List<Tupl> data = new();
            public List<WorktypePawn> excludedPawns = new();

            public List<(Priority priority, JobCount maxJobs, Dictionary<IWorkTypeWrapper, IPercent> workTypes)>
                ParsedData(IWorldInfoFacade serializer)
            {
                return data
                       .Select(x => x.Parsed(serializer))
                       .ToList();
            }

            public HashSet<(IWorkTypeWrapper, IPawnWrapper)> ParsedExcluded(IWorldInfoFacade serializer)
            {
                return excludedPawns
                       .Select(x => x.Parsed(serializer))
                       .Where(p => p.Item1 != null && p.Item2 != null)
                       .Select(p => (p.Item1!, p.Item2!))
                       .ToHashSet();
            }

            public static Ser Serialized(
                (List<(Priority priority, JobCount maxJobs, Dictionary<IWorkTypeWrapper, IPercent> workTypes)> percents,
                    HashSet<(IWorkTypeWrapper, IPawnWrapper)> excluded) data)
            {
                return new()
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
            public string pawnId = "";
            public string workType = "";

            public (IWorkTypeWrapper?, IPawnWrapper?) Parsed(IWorldInfoFacade serializer)
            {
                return (serializer.StringToDef(workType), serializer.IdToPawn(pawnId));
            }

            public static WorktypePawn Serialized((IWorkTypeWrapper work, IPawnWrapper pawn) data)
            {
                return new()
                {
                    workType = data.work.defName,
                    pawnId = data.pawn.ThingID
                };
            }
        }

        public class Tupl
        {
            public Dic dict = new();
            public int jobsMax;
            public int priority;

            public (Priority, JobCount, Dictionary<IWorkTypeWrapper, IPercent> ) Parsed(IWorldInfoFacade serializer)
            {
                return (priority, jobsMax, dict.Parsed(serializer));
            }

            public static Tupl Serialized((Priority, JobCount, Dictionary<IWorkTypeWrapper, IPercent>) val)
            {
                return new()
                {
                    priority = val.Item1.V,
                    jobsMax = val.Item2.V,
                    dict = Dic.Serialized(val.Item3)
                };
            }
        }

        public class Dic
        {
            public List<StrPercent> percents = new();

            public Dictionary<IWorkTypeWrapper, IPercent> Parsed(IWorldInfoFacade serializer)
            {
                return percents
                       .Select(x => x.Parsed(serializer))
                       .Where(x => x.Item1 != null)
                       .ToDictionary(x => x.Item1!, x => x.Item2);
            }

            public static Dic Serialized(Dictionary<IWorkTypeWrapper, IPercent> dic)
            {
                return new()
                {
                    percents = dic
                               .Select(kv => StrPercent.Serialized((kv.Key, kv.Value)))
                               .ToList()
                };
            }
        }

        public class StrPercent
        {
            public UnionPercent percent = new();
            public string workType = "";

            public (IWorkTypeWrapper?, IPercent) Parsed(IWorldInfoFacade serializer)
            {
                return (serializer.StringToDef(workType), percent.Parsed());
            }

            public static StrPercent Serialized((IWorkTypeWrapper work, IPercent percent) val)
            {
                return new()
                {
                    workType = val.work.defName,
                    percent = UnionPercent.Serialized(val.percent)
                };
            }
        }

        public class UnionPercent
        {
            public int number;
            public double percent;
            public Variant variant;

            // Controller.PoolPercents.Acquire(new PercentPoolArgs{Value = workType._percent})
            public IPercent Parsed()
            {
                return variant switch
                {
                    Variant.Number => Controller.PoolNumbers.Acquire(new NumberPoolArgs {Count = number, Total = 0}),
                    Variant.Percent => Controller.PoolPercents.Acquire(new PercentPoolArgs {Value = percent}),
                    _ => throw new Exception()
                };
            }

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
                    _ => throw new Exception()
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
