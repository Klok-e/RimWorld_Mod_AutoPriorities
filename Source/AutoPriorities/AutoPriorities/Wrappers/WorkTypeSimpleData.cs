using System;
using UnityEngine;
using Verse;

namespace AutoPriorities.Wrappers
{
    [Serializable]
    public record WorkTypeSimpleData
    {
        [SerializeField] public string? defName;
        [SerializeField] public WorkTags workTags;
        [SerializeField] public int relevantSkillsCount;
        [SerializeField] public string? labelShort;

        public WorkTypeSimpleData()
        {
        }

        public WorkTypeSimpleData(IWorkTypeWrapper workTypeWrapper)
        {
            defName = workTypeWrapper.DefName;
            workTags = workTypeWrapper.WorkTags;
            relevantSkillsCount = workTypeWrapper.RelevantSkillsCount;
            labelShort = workTypeWrapper.LabelShort;
        }
    }
}
