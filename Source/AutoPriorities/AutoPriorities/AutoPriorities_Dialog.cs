using AutoPriorities.Utils;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using AutoPriorities.Core;
using AutoPriorities.Percents;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AutoPriorities
{
    public class AutoPriorities_Dialog : Window
    {
        private HashSet<int> PrioritiesEncounteredCached { get; } = new HashSet<int>();

        private PawnsData PawnsData { get; } = new PawnsData();

        private PrioritiesAssigner PrioritiesAssigner { get; } = new PrioritiesAssigner();

        private const float SliderWidth = 20f;
        private const float SliderHeight = 60f;
        private const float SliderMargin = 75f;

        private const float GuiShadowedMult = 0.5f;

        private const float SlidersDistFromLeftBorder = 30f;
        private const float SlidersDistFromRightBorder = 10f;
        private const float DistFromBottomBorder = 50f;

        private const float ButtonHeight = 30f;
        private const float LabelHeight = 22f;
        private const float LabelMargin = 5f;
        private const float WorkLabelWidth = 75f;
        private const float WorkLabelOffset = 25f;
        private const float WorkLabelHorizOffset = 40f;

        private const float PercentStringWidth = 30f;
        private const float PercentStringLabelWidth = 20f;

        private Rect _rect;
        private bool _openedOnce;

        public AutoPriorities_Dialog()
        {
            doCloseButton = true;
            draggable = true;
            resizeable = true;
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

        private enum SelectedTab
        {
            Priorities = 1,
            PawnExclusion = 2,
        }

        private SelectedTab _currentlySelectedTab = SelectedTab.Priorities;

        private const string PrioritiesLabel = "Priorities";
        private readonly float _prioritiesLabelWidth = PrioritiesLabel.GetWidthCached() + 10f;

        private const string PawnExcludeLabel = "Exclude Colonists";
        private readonly float _pawnExcludeLabelWidth = PawnExcludeLabel.GetWidthCached() + 10f;

        private const string Label = "Run AutoPriorities";
        private readonly float _labelWidth = Label.GetWidthCached() + 10f;

        public override void DoWindowContents(Rect inRect)
        {
            // draw select tab buttons
            var prioritiesButtonRect =
                new Rect(inRect.xMin, inRect.yMin, _prioritiesLabelWidth, LabelHeight);
            if (Widgets.ButtonText(prioritiesButtonRect, PrioritiesLabel))
            {
                _currentlySelectedTab = SelectedTab.Priorities;
                PawnsData.Rebuild();
            }

            var pawnsButtonRect =
                new Rect(prioritiesButtonRect.xMax + 5f, prioritiesButtonRect.yMin,
                    _pawnExcludeLabelWidth, LabelHeight);
            if (Widgets.ButtonText(pawnsButtonRect, PawnExcludeLabel))
            {
                _currentlySelectedTab = SelectedTab.PawnExclusion;
                PawnsData.Rebuild();
            }

            // draw tab contents lower than buttons
            inRect.yMin += LabelHeight + 10f;

            // draw currently selected tab
            switch (_currentlySelectedTab)
            {
                case SelectedTab.Priorities:
                    PrioritiesTab(inRect);
                    break;
                case SelectedTab.PawnExclusion:
                    PawnExcludeTab(inRect);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_currentlySelectedTab));
            }

            // draw run auto priorities
            var buttonRect = new Rect(
                inRect.xMin,
                inRect.yMax - ButtonHeight,
                _labelWidth,
                ButtonHeight);
            if (Widgets.ButtonText(buttonRect, Label))
            {
                PawnsData.Rebuild();
                PrioritiesAssigner.AssignPriorities(PawnsData);
                PawnsData.SaveState();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private Vector2 _scrollPos;

        private void PrioritiesTab(Rect inRect)
        {
            var worktypes = PawnsData.SortedPawnFitnessForEveryWork.Count;

            var scrollRect = new Rect(inRect.xMin, inRect.yMin, inRect.width,
                inRect.height - DistFromBottomBorder);

            var tableSizeX = (worktypes + 1) * SliderMargin + SlidersDistFromLeftBorder + SlidersDistFromRightBorder;

            var tableSizeY = (SliderHeight + 3 * ButtonHeight) * PawnsData.WorkTables.Count;
            Widgets.BeginScrollView(scrollRect, ref _scrollPos, new Rect(0, 0, tableSizeX, tableSizeY));

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

                        var (available, takenMoreThanTotal) =
                            PawnsData.PercentColonistsAvailable(workType, pr.priority);
                        var prevCol = GUI.color;
                        if (takenMoreThanTotal)
                            GUI.color = Color.red;

                        var labelRect = new Rect(elementXPos - workName.GetWidthCached() / 2,
                            slidersRect.yMin + (i % 2 == 0 ? 0f : 20f) + 10f, 100f, LabelHeight);
                        Widgets.Label(labelRect, workName);

                        GUI.color = prevCol;

                        var sliderRect = new Rect(elementXPos, slidersRect.yMin + 60f, SliderWidth, SliderHeight);
                        var currSliderVal = (float) pr.workTypes[workType].Value;
                        var newSliderValue =
                            GUI.VerticalSlider(sliderRect, currSliderVal, Math.Max(1f, currSliderVal), 0f);

                        newSliderValue = Mathf.Clamp(newSliderValue, 0f, Math.Max((float) available, currSliderVal));

                        var percentsText = (currentPercent switch
                        {
                            Percent _ => Mathf.RoundToInt(newSliderValue * 100f),
                            Number _ => Mathf.RoundToInt(newSliderValue * PawnsData.NumberColonists(workType)),
                            _ => throw new ArgumentOutOfRangeException(nameof(currentPercent))
                        }).ToString();
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

#if DEBUG
                        if (sliderValRepr > 0f)
                        {
                            // Controller.Log!.Trace(
                            //     $"sliderValRepr for {workType} worktype and {pr.priority} " +
                            //     $"priority is {sliderValRepr}, newSliderValue is {newSliderValue}");
                        }
#endif
                        prevCol = GUI.color;
                        if (takenMoreThanTotal)
                            GUI.color = Color.red;

                        Widgets.TextFieldNumeric(percentsRect, ref sliderValRepr, ref percentsText);

                        GUI.color = prevCol;

                        var prevSliderValText = newSliderValue;

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
                                        Count = Mathf.RoundToInt(newSliderValue * PawnsData.NumberColonists(workType)),
                                        Total = PawnsData.NumberColonists(workType)
                                    });
                                }

                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(currentPercent));
                        }

                        newSliderValue = Mathf.Clamp(newSliderValue, 0f,
                            Math.Max((float) available, prevSliderValText));

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
                                    Count = Mathf.RoundToInt(newSliderValue * PawnsData.NumberColonists(workType)),
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

        private Vector2 _pawnExcludeScrollPos;
        private const float PawnNameCoWidth = 150f;

        private void PawnExcludeTab(Rect inRect)
        {
            const float fromTopToTickboxesVertical = WorkLabelOffset + LabelHeight + 15f;

            var scrollRect = new Rect(inRect.xMin, inRect.yMin, inRect.width,
                inRect.height - DistFromBottomBorder);

            var tableSizeX = PawnNameCoWidth + WorkLabelWidth / 2 +
                             WorkLabelHorizOffset * PawnsData.WorkTypes.Count;

            var tableSizeY = fromTopToTickboxesVertical + (LabelMargin + ButtonHeight) * PawnsData.AllPlayerPawns.Count;
            Widgets.BeginScrollView(scrollRect, ref _pawnExcludeScrollPos, new Rect(0, 0, tableSizeX, tableSizeY));

            var tickboxesRect = new Rect(PawnNameCoWidth, fromTopToTickboxesVertical,
                tableSizeX - PawnNameCoWidth, tableSizeY - fromTopToTickboxesVertical);
            var anchor = Text.Anchor;
#if DEBUG
            //Widgets.DrawBox(tickboxesRect); 
#endif
            // draw worktypes
            Text.Anchor = TextAnchor.UpperCenter;
            foreach (var (workType, i) in PawnsData.WorkTypes.Zip(
                Enumerable.Range(0, PawnsData.WorkTypes.Count),
                (w, i) => (w, i)))
            {
                var workLabel = workType.labelShort;
                var rect = new Rect(tickboxesRect.xMin + WorkLabelHorizOffset * i, i % 2 == 0 ? 0f : WorkLabelOffset,
                    WorkLabelWidth,
                    LabelHeight);
                Widgets.Label(rect, workLabel);
#if DEBUG
                //Widgets.DrawBox(rect);
#endif
                var horizLinePos = rect.center.x;
                Widgets.DrawLine(new Vector2(horizLinePos, rect.yMax),
                    new Vector2(horizLinePos, tickboxesRect.yMin),
                    Color.grey, 1f);
            }

            Text.Anchor = TextAnchor.UpperLeft;
            foreach (var (pawn, rowi) in PawnsData.AllPlayerPawns.Zip(
                Enumerable.Range(0, PawnsData.AllPlayerPawns.Count),
                (w, i) => (w, i)))
            {
                // draw pawn name
                var nameRect = new Rect(0f, tickboxesRect.yMin + (LabelMargin + ButtonHeight) * rowi,
                    PawnNameCoWidth, LabelHeight + LabelMargin);
                Widgets.Label(nameRect, pawn.LabelNoCountColored);
                TooltipHandler.TipRegion(nameRect, "Click here to toggle all jobs");
                if (Widgets.ButtonInvisible(nameRect))
                {
                    var c = PawnsData.ExcludedPawns.Count(x => x.Item2 == pawn);
                    if (c > PawnsData.WorkTypes.Count / 2)
                        PawnsData.ExcludedPawns.RemoveWhere(x => x.Item2 == pawn);
                    else
                        foreach (var work in PawnsData.WorkTypes)
                            PawnsData.ExcludedPawns.Add((work, pawn));
                }

                Widgets.DrawLine(new Vector2(nameRect.xMin, nameRect.yMax),
                    new Vector2(tickboxesRect.xMax, nameRect.yMax),
                    Color.grey, 1f);

                // draw tickboxes
                foreach (var (workType, i) in PawnsData.WorkTypes.Zip(
                    Enumerable.Range(0, PawnsData.WorkTypes.Count),
                    (w, i) => (w, i)))
                {
                    var prev = PawnsData.ExcludedPawns.Contains((workType, pawn));
                    var next = prev;
                    DrawUtil.EmptyCheckbox(nameRect.xMax - (ButtonHeight - 1) / 2 + (i + 1) * WorkLabelHorizOffset,
                        nameRect.yMin, ref next);
                    if (prev == next) continue;
                    if (next)
                    {
                        PawnsData.ExcludedPawns.Add((workType, pawn));
#if DEBUG
                        Controller.Log!.Message(
                            $"Pawn {pawn.NameFullColored} with work {workType.defName} was added to the Excluded list");
#endif
                    }
                    else
                    {
                        PawnsData.ExcludedPawns.Remove((workType, pawn));
#if DEBUG
                        Controller.Log!.Message(
                            $"Pawn {pawn.NameFullColored} with work {workType.defName} was removed from the Excluded list");
#endif
                    }
                }
            }

            Widgets.EndScrollView();

            Text.Anchor = anchor;
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