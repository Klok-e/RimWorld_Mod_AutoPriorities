using AutoPriorities.Utils;
using RimWorld;
using System;
using System.Collections.Generic;
using AutoPriorities.Core;
using AutoPriorities.Percents;
using HugsLib.Logs;
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

        private const float SliderWidth = 20f;
        private const float SliderHeight = 60f;
        private const float SliderMargin = 75f;

        private const float GuiShadowedMult = 0.5f;

        private const float SlidersDistFromLeftBorder = 30f;
        private const float SlidersDistFromRightBorder = 10f;
        private const float DistFromTopBorder = 80f;
        private const float DistFromBottomBorder = 50f;

        private const float ScrollSize = 30f;

        private const float ButtonHeight = 30f;

        private const float PercentStringWidth = 30f;
        private const float PercentStringLabelWidth = 20f;

        private Vector2 _scrollPos;
        private Rect _rect;
        private bool _openedOnce;

        public AutoPriorities_Dialog()
        {
            doCloseButton = true;
            draggable = true;
            resizeable = true;
            _scrollPos = new Vector2();
            PrioritiesEncounteredCached = new HashSet<int>();
            _openedOnce = false;

            PawnsData = new PawnsData();
            PrioritiesAssigner = new PrioritiesAssigner();
        }

        public override void PostClose()
        {
            base.PostClose();
            _rect = windowRect;
            PawnsData.SaveState();
        }

        public override void PostOpen()
        {
            base.PostOpen();
            if (_openedOnce)
                windowRect = _rect;
            else
                _openedOnce = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var worktypes = PawnsData.SortedPawnFitnessForEveryWork.Count;

            var scrollRect = new Rect(inRect.xMin, inRect.yMin, inRect.width,
                inRect.height - DistFromBottomBorder);

            var tableSizeX = (worktypes + 1) * SliderMargin + SlidersDistFromLeftBorder + SlidersDistFromRightBorder;
            var scrollWidth = tableSizeX > inRect.width ? tableSizeX : inRect.width;

            var tableSizeY = (SliderHeight + 3 * ButtonHeight) * PawnsData.WorkTables.Count;
            var scrollHeight = tableSizeY > scrollRect.height ? tableSizeY : inRect.height;
            Widgets.BeginScrollView(scrollRect, ref _scrollPos, new Rect(0, 0, scrollWidth, scrollHeight));

            PrioritiesEncounteredCached.Clear();
            var row = 0;
            var workTables = PawnsData.WorkTables;
            for (var table = 0; table < workTables.Count; table++)
            {
                // table row
                var pr = PawnsData.WorkTables[table];
                var colOrig = GUI.color;
                //shadow repeating priorities
                if (PrioritiesEncounteredCached.Contains(pr.priority))
                    GUI.color = colOrig * GuiShadowedMult;

                var slidersRect = new Rect(
                    SlidersDistFromLeftBorder,
                    (SliderHeight + 3 * ButtonHeight) * row,
                    tableSizeX + SlidersDistFromRightBorder,
                    SliderHeight + 3 * ButtonHeight + 5f
                );

                //draw bottom line
                Widgets.DrawLine(new Vector2(slidersRect.xMin, slidersRect.yMax),
                    new Vector2(slidersRect.xMax, slidersRect.yMax), new Color(0.7f, 0.7f, 0.7f), 1f);

                pr.priority = DrawUtil.PriorityBox(slidersRect.xMin, slidersRect.yMin + (slidersRect.height / 2f),
                    pr.priority);
                PawnsData.WorkTables[table] = pr;

                var i = 0;
                foreach (var workType in PawnsData.WorkTypes)
                {
                    var workName = workType.labelShort;
                    try
                    {
                        var currentPercent = pr.workTypes[workType];

                        var elementXPos = slidersRect.xMin + SliderMargin * (i + 1);

                        var labelRect = new Rect(elementXPos - (workName.GetWidthCached() / 2),
                            slidersRect.yMin + (i % 2 == 0 ? 0f : 20f) + 10f, 100f, 25f);
                        Widgets.Label(labelRect, workName);

                        var sliderRect = new Rect(elementXPos, slidersRect.yMin + 60f, SliderWidth, SliderHeight);
                        var newSliderValue =
                            GUI.VerticalSlider(sliderRect, (float) pr.workTypes[workType].Value, 1f, 0f);
                        var available = (float) PawnsData.PercentColonistsAvailable(workType, pr.priority);
                        newSliderValue = Mathf.Min(available, newSliderValue);

                        var percentsText = currentPercent switch
                        {
                            Percent _ => ((int) (newSliderValue * 100f)).ToString(),
                            Number _ => ((int) (newSliderValue * PawnsData.NumberColonists(workType))).ToString(),
                            _ => throw new ArgumentOutOfRangeException(nameof(currentPercent))
                        };
                        var percentsRect = new Rect(
                            sliderRect.xMax - PercentStringWidth,
                            sliderRect.yMax + 3f,
                            PercentStringWidth,
                            25f);

                        // percents and numbers switch button
                        var switchRect = new Rect(percentsRect.min +
                                                  new Vector2(5f + PercentStringLabelWidth, 0f), percentsRect.size);

                        var sliderValRepr = currentPercent switch
                        {
                            Percent _ => newSliderValue * 100f,
                            Number _ => newSliderValue * PawnsData.NumberColonists(workType),
                            _ => throw new ArgumentOutOfRangeException(nameof(currentPercent))
                        };
                     
                        Widgets.TextFieldNumeric(percentsRect, ref sliderValRepr, ref percentsText);
                        var symbolRect = new Rect(switchRect.min + new Vector2(5f, 0f), switchRect.size);
                        switch (currentPercent)
                        {
                            case Number n:
                                newSliderValue = sliderValRepr / PawnsData.NumberColonists(workType);
                                if (Widgets.ButtonText(symbolRect, "№"))
                                {
                                    Controller.PoolNumbers.Pool(n);
                                    currentPercent = Controller.PoolPercents.Acquire(new PercentPoolArgs
                                    {
                                        Value = newSliderValue
                                    });
                                }

                                break;
                            case Percent p:
                                newSliderValue = sliderValRepr / 100f;
                                if (Widgets.ButtonText(symbolRect, "%"))
                                {
                                    Controller.PoolPercents.Pool(p);
                                    currentPercent = Controller.PoolNumbers.Acquire(new NumberPoolArgs
                                    {
                                        Count = (int) (newSliderValue * PawnsData.NumberColonists(workType)),
                                        Total = PawnsData.NumberColonists(workType)
                                    });
                                }

                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(currentPercent));
                        }

                        newSliderValue = Mathf.Min(available, newSliderValue);

                        switch (currentPercent)
                        {
                            case Percent p:
                                Controller.PoolPercents.Pool(p);
                                pr.workTypes[workType] = Controller.PoolPercents.Acquire(new PercentPoolArgs
                                {
                                    Value = newSliderValue
                                });
                                break;
                            case Number n:
                                Controller.PoolNumbers.Pool(n);
                                pr.workTypes[workType] = Controller.PoolNumbers.Acquire(new NumberPoolArgs
                                {
                                    Count = (int) (newSliderValue * PawnsData.NumberColonists(workType)),
                                    Total = PawnsData.NumberColonists(workType)
                                });
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error for work type {workName}:");
                        e.LogStackTrace();
                    }

                    i += 1;
                }

                row += 1;
                PrioritiesEncounteredCached.Add(pr.priority);
                //return to normal
                GUI.color = colOrig;
            }

            Widgets.EndScrollView();

            const string label = "Run AutoPriorities";
            var buttonRect = new Rect(
                inRect.xMin,
                scrollRect.yMax + 9f,
                label.GetWidthCached() + 10f,
                ButtonHeight);
            if (Widgets.ButtonText(buttonRect, label))
            {
                PawnsData.Rebuild();
                PrioritiesAssigner.AssignPriorities(PawnsData);
                PawnsData.SaveState();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }

            var removePriorityButtonRect = new Rect(
                inRect.xMax - SliderMargin,
                scrollRect.yMax + 9f,
                ButtonHeight,
                ButtonHeight);
            if (Widgets.ButtonImage(removePriorityButtonRect, Core.Resources._minusIcon))
            {
                RemovePriority();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }

            var addPriorityButtonRect = new Rect(
                removePriorityButtonRect.xMin - StandardMargin - removePriorityButtonRect.width,
                scrollRect.yMax + 9f,
                ButtonHeight,
                ButtonHeight);
            if (Widgets.ButtonImage(addPriorityButtonRect, Core.Resources._plusIcon))
            {
                AddPriority();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private void AddPriority()
        {
            var dict = new Dictionary<WorkTypeDef, IPercent>();
            PawnsData.WorkTables.Add((0, dict));

            foreach (var keyValue in PawnsData.WorkTypes)
                dict.Add(keyValue, Controller.PoolPercents.Acquire(new PercentPoolArgs {Value = 0}));
        }

        private void RemovePriority()
        {
            if (PawnsData.WorkTables.Count > 0)
                PawnsData.WorkTables.RemoveLast();
        }
    }
}