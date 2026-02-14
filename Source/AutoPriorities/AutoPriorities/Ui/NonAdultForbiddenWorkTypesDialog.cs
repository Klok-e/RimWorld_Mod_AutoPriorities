using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AutoPriorities.Ui
{
    public class NonAdultForbiddenWorkTypesDialog : Window
    {
        private Vector2 _scrollPosition;

        public NonAdultForbiddenWorkTypesDialog()
        {
            doCloseButton = true;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new(350f, 400f);

        public override void DoWindowContents(Rect inRect)
        {
            var settings = AutoPrioritiesMod.Settings;
            if (settings == null)
                return;

            var titleRect = new Rect(inRect.xMin, inRect.yMin, inRect.width, Consts.LabelHeight);
            Widgets.Label(titleRect, Consts.NonAdultForbiddenWorkTypesTitle);

            var selectedDefs =
                (settings.nonAdultForbiddenWorkTypeDefNames ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet();

            var workTypes = WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder.ToList();
            var listHeight = workTypes.Count * (Consts.LabelHeight + Consts.LabelMargin);

            var scrollOuterRect =
                new Rect(
                    inRect.xMin,
                    titleRect.yMax + Consts.LabelMargin,
                    inRect.width,
                    inRect.height - titleRect.height - Consts.LabelMargin - Consts.DistFromBottomBorder
                );
            var scrollInnerRect = new Rect(0f, 0f, scrollOuterRect.width - 16f, listHeight);

            Widgets.BeginScrollView(scrollOuterRect, ref _scrollPosition, scrollInnerRect);

            var y = 0f;
            foreach (var workTypeDef in workTypes)
            {
                var rowRect = new Rect(0f, y, scrollInnerRect.width, Consts.LabelHeight);
                var selected = selectedDefs.Contains(workTypeDef.defName);
                Widgets.CheckboxLabeled(rowRect, workTypeDef.defName, ref selected);

                if (selected)
                    selectedDefs.Add(workTypeDef.defName);
                else
                    selectedDefs.Remove(workTypeDef.defName);

                y += Consts.LabelHeight + Consts.LabelMargin;
            }

            Widgets.EndScrollView();

            settings.nonAdultForbiddenWorkTypeDefNames = selectedDefs.ToList();
        }
    }
}
