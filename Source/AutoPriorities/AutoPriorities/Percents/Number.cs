using System;
using AutoPriorities.Utils;
using UnityEngine;
using UnityEngine.Serialization;
using Verse;

namespace AutoPriorities.Percents
{
    public class Number : IPercent, IPoolable<Number,NumberPoolArgs>
    {
        public int Total { get; private set; }

        public int Count { get; private set; }

        public Variant Variant => Variant.Number;
        public double Value => (double) Count / Total;

        public Number Initialize(NumberPoolArgs args)
        {
            Total = args.Total;
            Count = args.Count;
            return this;
        }

        public void Deinitialize()
        {
        }
    }

    public struct NumberPoolArgs : IPoolArgs
    {
        public int Count { get; set; }
        public int Total { get; set; }
    }
}