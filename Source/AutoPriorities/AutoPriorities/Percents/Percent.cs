using System;
using AutoPriorities.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace AutoPriorities.Percents
{
    public class Percent : IPercent, IPoolable<Percent, PercentPoolArgs>
    {
        public double Value { get; private set; }

        public Percent Initialize(PercentPoolArgs args)
        {
            Value = args.Value;
            return this;
        }

        public void Deinitialize()
        {
        }
    }

    public struct PercentPoolArgs : IPoolArgs
    {
        public double Value { get; set; }
    }
}
