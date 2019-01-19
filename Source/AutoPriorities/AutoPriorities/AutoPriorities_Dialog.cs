using AutoPriorities.Utils;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AutoPriorities
{
    public class AutoPriorities_Dialog : Window
    {
        private HashSet<int> PrioritiesEncounteredCached { get; }

        private PawnsData PawnsData { get; }

        private PrioritiesAssigner PrioritiesAssigner { get; }

        private const float _sliderWidth = 20f;
        private const float _sliderHeight = 60f;
        private const float _sliderMargin = 50f;

        private const float _guiShadowedMult = 0.5f;

        private const float _slidersDistFromLeftBorder = 30f;
        private const float _slidersDistFromRightBorder = 30f;
        private const float _distFromTopBorder = 80f;
        private const float _distFromBottomBorder = 50f;

        private const float _scrollSize = 30f;

        private const float _buttonHeight = 30f;

        private Vector2 _scrollPos;
        private Rect _rect;
        private bool _openedOnce;

        public AutoPriorities_Dialog() : base()
        {
            doCloseButton = true;
            draggable = true;
            resizeable = true;
            _scrollPos = new Vector2();
            PrioritiesEncounteredCached = new HashSet<int>();
            _openedOnce = false;

            PawnsData = new PawnsData();
            PrioritiesAssigner = new PrioritiesAssigner(PawnsData);
        }

        public override void PostClose()
        {
            base.PostClose();
            _rect = windowRect;
        }

        public override void PostOpen()
        {
            base.PostOpen();
            if(_openedOnce)
            {
                PawnsData.Rebuild();
                windowRect = _rect;
            }
            else
                _openedOnce = true;
        }

        public override void DoWindowContents(Rect windowRect)
        {
            var worktypes = PawnsData.SortedPawnSkillForEveryWork.Count;

            var scrollRect = new Rect(windowRect.xMin, windowRect.yMin, windowRect.width, windowRect.height - _distFromBottomBorder);

            var tableSizeX = (worktypes + 1) * _sliderMargin + _slidersDistFromLeftBorder + _slidersDistFromRightBorder;
            var scrollWidth = tableSizeX > windowRect.width ? tableSizeX : windowRect.width;

            var tableSizeY = (_sliderHeight + 3 * _buttonHeight) * PawnsData.PriorityToWorkTypesAndPercentOfPawns.Count;
            var scrollHeight = tableSizeY > scrollRect.height ? tableSizeY : windowRect.height;
            Widgets.BeginScrollView(scrollRect, ref _scrollPos, new Rect(0, 0, scrollWidth, scrollHeight));

            PrioritiesEncounteredCached.Clear();
            int row = 0;
            foreach(var pr in PawnsData.PriorityToWorkTypesAndPercentOfPawns)
            {
                var colOrig = GUI.color;
                //shadow repeating priorities
                if(PrioritiesEncounteredCached.Contains(pr._val1))
                    GUI.color = colOrig * _guiShadowedMult;

                var slidersRect = new Rect(_slidersDistFromLeftBorder, (_sliderHeight + 3 * _buttonHeight) * row, tableSizeX + _slidersDistFromRightBorder, _sliderHeight + 3 * _buttonHeight);

                //draw bottom line
                Widgets.DrawLine(new Vector2(slidersRect.xMin, slidersRect.yMax), new Vector2(slidersRect.xMax, slidersRect.yMax), new Color(0.7f, 0.7f, 0.7f), 1f);

                var priorityButtonRect = new Rect(slidersRect.xMin, slidersRect.yMin + (slidersRect.height / 2f), _buttonHeight, _buttonHeight);

                pr._val1 = DrawUtil.PriorityBox(slidersRect.xMin, slidersRect.yMin + (slidersRect.height / 2f), pr._val1);

                int i = 0;
                foreach(var workType in PawnsData.WorkTypes)
                {
                    var workName = workType.labelShort;
                    try
                    {
                        float elementXPos = slidersRect.xMin + _sliderMargin * (i + 1);

                        var labelRect = new Rect(elementXPos - (workName.GetWidthCached() / 2), slidersRect.yMin + (i % 2 == 0 ? 0f : 30f), 100f, 30f);
                        Widgets.Label(labelRect, workName);

                        var sliderRect = new Rect(elementXPos, slidersRect.yMin + 60f, _sliderWidth, _sliderHeight);
                        var newSliderValue = GUI.VerticalSlider(sliderRect, pr._val2[workType], 1f, 0f);
                        var available = PawnsData.PercentOfColonistsAvailable(workType, pr._val1);
                        if(available < newSliderValue)
                            newSliderValue = available;
                        pr._val2[workType] = newSliderValue;

                        var percentLabelText = Mathf.RoundToInt(pr._val2[workType] * 100f).ToString() + "%";
                        var percentLabelRect = new Rect(
                            sliderRect.xMax - (_sliderWidth / 2) - (percentLabelText.GetWidthCached() / 2),
                            sliderRect.yMax,
                            percentLabelText.GetWidthCached(),
                            25f);
                        Widgets.Label(percentLabelRect, percentLabelText);

                        i += 1;
                    }
                    catch(Exception e)
                    {
                        Log.Error($"Error {e.Message} for work type {workName}");
                    }
                }
                row += 1;
                PrioritiesEncounteredCached.Add(pr._val1);
                //return to normal
                GUI.color = colOrig;
            }
            Widgets.EndScrollView();

            var label = "Run AutoPriorities";
            var buttonRect = new Rect(
                windowRect.xMin,
                scrollRect.yMax + 9f,
                label.GetWidthCached() + 10f,
                _buttonHeight);
            if(Widgets.ButtonText(buttonRect, label))
            {
                PrioritiesAssigner.AssignPriorities();
                PawnsData.SaveState();
                SoundDefOf.AmountIncrement.PlayOneShotOnCamera();
            }

            var removePriorityButtonRect = new Rect(
                windowRect.xMax - _sliderMargin,
                scrollRect.yMax + 9f,
                _buttonHeight,
                _buttonHeight);
            if(Widgets.ButtonImage(removePriorityButtonRect, Core.Resources._minusIcon))
            {
                RemovePriority();
                SoundDefOf.AmountIncrement.PlayOneShotOnCamera();
            }

            var addPriorityButtonRect = new Rect(
                removePriorityButtonRect.xMin - StandardMargin - removePriorityButtonRect.width,
                scrollRect.yMax + 9f,
                _buttonHeight,
                _buttonHeight);
            if(Widgets.ButtonImage(addPriorityButtonRect, Core.Resources._plusIcon))
            {
                AddPriority();
                SoundDefOf.AmountIncrement.PlayOneShotOnCamera();
            }
        }

        private void AddPriority()
        {
            var dict = new Dictionary<WorkTypeDef, float>();
            PawnsData.PriorityToWorkTypesAndPercentOfPawns.Add(new Tuple2<int, Dictionary<WorkTypeDef, float>>(0, dict));

            foreach(var keyValue in PawnsData.WorkTypes)
                dict.Add(keyValue, 0f);
        }

        private void RemovePriority()
        {
            if(PawnsData.PriorityToWorkTypesAndPercentOfPawns.Count > 0)
                PawnsData.PriorityToWorkTypesAndPercentOfPawns.RemoveLast();
        }
    }
}
