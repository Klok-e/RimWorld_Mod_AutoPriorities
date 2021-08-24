using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoPriorities.Core;
using AutoPriorities.ImportantJobs;
using AutoPriorities.PawnDataSerializer.Exporter;
using AutoPriorities.Percents;
using AutoPriorities.Utils;
using AutoPriorities.Wrappers;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using ILogger = AutoPriorities.APLogger.ILogger;
using Resources = AutoPriorities.Core.Resources;

namespace AutoPriorities.Ui
{
    public class AutoPrioritiesDialog : Window
    {
        private const float SliderWidth = 20f;
        private const float SliderHeight = 60f;
        private const float SliderMargin = 75f;
        private const float GuiShadowedMult = 0.5f;
        private const float SlidersDistFromLeftBorder = 30f;
        private const float SlidersDistFromRightBorder = 20f;
        private const float DistFromBottomBorder = 50f;
        private const float ButtonHeight = 30f;
        private const float LabelHeight = 22f;
        private const float LabelMargin = 5f;
        private const float WorkLabelWidth = 75f;
        private const float WorkLabelOffset = 25f;
        private const float WorkLabelHorizOffset = 40f;
        private const float PercentStringWidth = 30f;
        private const float PercentStringLabelWidth = 20f;
        private const string PrioritiesLabel = "Priorities";
        private const string PawnExcludeLabel = "Exclude Colonists";
        private const string ImportantJobsLabel = "Important Jobs";
        private const string Label = "Run AutoPriorities";
        private const string DeleteLabel = "Delete";
        private const string ExportLabel = "Export";
        private const string ImportLabel = "Import";
        private const float PawnNameCoWidth = 150f;
        private readonly float _importantJobsLabelWidth = ImportantJobsLabel.GetWidthCached() + 10f;
        private readonly IImportantJobsProvider _importantJobsProvider;
        private readonly float _importExportImportLabelWidth = DeleteLabel.GetWidthCached() + 10f;
        private readonly float _labelWidth = Label.GetWidthCached() + 10f;
        private readonly ILogger _logger;
        private readonly IPawnDataExporter _pawnDataExporter;
        private readonly float _pawnExcludeLabelWidth = PawnExcludeLabel.GetWidthCached() + 10f;
        private readonly PawnsData _pawnsData;
        private readonly PrioritiesAssigner _prioritiesAssigner;
        private readonly HashSet<Priority> _prioritiesEncounteredCached = new();
        private readonly float _prioritiesLabelWidth = PrioritiesLabel.GetWidthCached() + 10f;
        private readonly Dictionary<Rect, string?> _textFieldBuffers = new();
        private SelectedTab _currentlySelectedTab = SelectedTab.Priorities;
        private bool _openedOnce;
        private Vector2 _pawnExcludeScrollPos;
        private Rect _rect;

        private Vector2 _scrollPos;
        // private QuickProfilerFactory _profilerFactory = new();
        // private int _windowContentsCalls;

        public AutoPrioritiesDialog(PawnsData pawnsData,
            PrioritiesAssigner prioritiesAssigner,
            ILogger logger,
            IImportantJobsProvider importantJobsProvider,
            IPawnDataExporter pawnDataExporter)
        {
            _pawnsData = pawnsData;
            _prioritiesAssigner = prioritiesAssigner;
            _logger = logger;
            _importantJobsProvider = importantJobsProvider;
            _pawnDataExporter = pawnDataExporter;
            doCloseButton = true;
            draggable = true;
            resizeable = true;
        }

        public override void PostClose()
        {
            base.PostClose();
            _rect = windowRect;
            _pawnsData.SaveState();
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
            // using (_profilerFactory.CreateProfiler("DoWindowContents"))
            {
                // draw select tab buttons
                var prioritiesButtonRect = new Rect(inRect.xMin, inRect.yMin, _prioritiesLabelWidth, LabelHeight);
                if (Widgets.ButtonText(prioritiesButtonRect, PrioritiesLabel))
                {
                    _currentlySelectedTab = SelectedTab.Priorities;
                    _pawnsData.Rebuild();
                    _textFieldBuffers.Clear();
                }

                var pawnsButtonRect = new Rect(
                    prioritiesButtonRect.xMax + 5f,
                    prioritiesButtonRect.yMin,
                    _pawnExcludeLabelWidth,
                    LabelHeight);
                if (Widgets.ButtonText(pawnsButtonRect, PawnExcludeLabel))
                {
                    _currentlySelectedTab = SelectedTab.PawnExclusion;
                    _pawnsData.Rebuild();
                    _textFieldBuffers.Clear();
                }

                var importantButtonRect = new Rect(
                    pawnsButtonRect.xMax + 5f,
                    prioritiesButtonRect.yMin,
                    _importantJobsLabelWidth,
                    LabelHeight);
                if (Widgets.ButtonText(importantButtonRect, ImportantJobsLabel))
                {
                    _currentlySelectedTab = SelectedTab.ImportantWorkTypes;
                    _pawnsData.Rebuild();
                    _textFieldBuffers.Clear();
                }

                // draw tab contents lower than buttons
                var lowerInRect = inRect;
                lowerInRect.yMin += LabelHeight + 10f;

                // draw currently selected tab
                switch (_currentlySelectedTab)
                {
                    case SelectedTab.Priorities:
                        PrioritiesTab(lowerInRect);
                        break;
                    case SelectedTab.PawnExclusion:
                        PawnExcludeTab(lowerInRect);
                        break;
                    case SelectedTab.ImportantWorkTypes:
                        ImportantWorkTypes(lowerInRect);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_currentlySelectedTab));
                }

                var buttonDeleteRect = new Rect(
                    inRect.xMax - _importExportImportLabelWidth,
                    inRect.yMin,
                    _importExportImportLabelWidth,
                    LabelHeight);
                DrawDeleteButton(buttonDeleteRect);

                var buttonImportRect = new Rect(
                    buttonDeleteRect.xMin - _importExportImportLabelWidth,
                    inRect.yMin,
                    _importExportImportLabelWidth,
                    LabelHeight);
                DrawImportButton(buttonImportRect);

                var buttonExportRect = new Rect(
                    buttonImportRect.xMin - _importExportImportLabelWidth,
                    inRect.yMin,
                    _importExportImportLabelWidth,
                    LabelHeight);
                DrawExportButton(buttonExportRect);

                var buttonRunRect = new Rect(inRect.xMin, inRect.yMax - ButtonHeight, _labelWidth, ButtonHeight);
                DrawRunButton(buttonRunRect);
            }

            // if (_windowContentsCalls % 1000 == 0) _profilerFactory.SaveProfileData();

            // _windowContentsCalls += 1;
        }

        private void DrawRunButton(Rect inRect)
        {
            if (Widgets.ButtonText(inRect, Label))
            {
                _pawnsData.Rebuild();
                _prioritiesAssigner.AssignPriorities();
                _pawnsData.SaveState();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private void DrawImportButton(Rect inRect)
        {
            var saves = _pawnDataExporter.ListSaves()
                                         .ToList();
            if (saves.Any() && Widgets.ButtonText(inRect, ImportLabel))
            {
                var options = saves.Select(
                                       x => new FloatMenuOption(
                                           x,
                                           () =>
                                           {
                                               _pawnDataExporter.ImportPawnData(x);
                                               _textFieldBuffers.Clear();
                                           }))
                                   .ToList();
                Find.WindowStack.Add(new FloatMenu(options, string.Empty));
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private void DrawExportButton(Rect inRect)
        {
            if (Widgets.ButtonText(inRect, ExportLabel))
            {
                Find.WindowStack.Add(new NameExportDialog(_pawnDataExporter));
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private void DrawDeleteButton(Rect inRect)
        {
            var saves = _pawnDataExporter.ListSaves()
                                         .ToList();
            if (saves.Any() && Widgets.ButtonText(inRect, DeleteLabel))
            {
                var options = saves.Select(x => new FloatMenuOption(x, () => _pawnDataExporter.DeleteSave(x)))
                                   .ToList();
                Find.WindowStack.Add(new FloatMenu(options, string.Empty));
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private void ImportantWorkTypes(Rect inRect)
        {
            const float fromTopToTickboxesVertical = WorkLabelOffset + LabelHeight + 15f;

            var scrollRect = new Rect(inRect.xMin, inRect.yMin, inRect.width, inRect.height - DistFromBottomBorder);

            var tableSizeX = WorkLabelWidth / 2 + WorkLabelHorizOffset * _pawnsData.WorkTypesNotRequiringSkills.Count;

            var tableSizeY = fromTopToTickboxesVertical + (LabelMargin + ButtonHeight);
            Widgets.BeginScrollView(scrollRect, ref _pawnExcludeScrollPos, new Rect(0, 0, tableSizeX, tableSizeY));

            var tickboxesRect = new Rect(
                0,
                fromTopToTickboxesVertical,
                tableSizeX,
                tableSizeY - fromTopToTickboxesVertical);
            var anchor = Text.Anchor;

            // Widgets.DrawBox(tickboxesRect);

            var workTypes = _importantJobsProvider.ImportantWorkTypes();

            // draw worktypes
            Text.Anchor = TextAnchor.UpperCenter;
            foreach (var (workType, i) in _pawnsData.WorkTypesNotRequiringSkills.Select((w, i) => (w, i)))
            {
                var workLabel = workType.LabelShort;
                var rect = new Rect(
                    tickboxesRect.xMin + WorkLabelHorizOffset * i,
                    i % 2 == 0 ? 0f : WorkLabelOffset,
                    WorkLabelWidth,
                    LabelHeight);
                Widgets.Label(rect, workLabel);

                // Widgets.DrawBox(rect);

                var horizLinePos = rect.center.x;
                Widgets.DrawLine(
                    new Vector2(horizLinePos, rect.yMax),
                    new Vector2(horizLinePos, tickboxesRect.yMin),
                    Color.grey,
                    1f);

                var prev = workTypes.Contains(workType);
                var next = prev;
                DrawUtil.EmptyCheckbox(
                    (ButtonHeight - 1) / 2 + i * WorkLabelHorizOffset + 11f,
                    tickboxesRect.yMin,
                    ref next);
                if (prev == next) continue;

                if (next)
                    workTypes.Add(workType);
                else
                    workTypes.Remove(workType);

                _importantJobsProvider.SaveImportantWorkTypes(workTypes.Select(x => x.DefName));
            }

            Widgets.EndScrollView();

            Text.Anchor = anchor;
        }

        private void PrioritiesTab(Rect inRect)
        {
            // using (_profilerFactory.CreateProfiler("PrioritiesTab"))
            var workTypes = _pawnsData.SortedPawnFitnessForEveryWork.Count;

            var scrollRect = new Rect(inRect.xMin, inRect.yMin, inRect.width, inRect.height - DistFromBottomBorder);

            var tableSizeX = (workTypes + 1) * SliderMargin + SlidersDistFromLeftBorder + SlidersDistFromRightBorder
                             + SliderMargin;

            var tableSizeY = (SliderHeight + 3 * ButtonHeight) * _pawnsData.WorkTables.Count;
            Widgets.BeginScrollView(scrollRect, ref _scrollPos, new Rect(0, 0, tableSizeX, tableSizeY));

            _prioritiesEncounteredCached.Clear();
            var workTables = _pawnsData.WorkTables;
            for (int table = 0, row = 0; table < workTables.Count; table++, row++)
            {
                // table row
                var pr = workTables[table];
                var colOrig = GUI.color;

                //shadow repeating priorities
                if (_prioritiesEncounteredCached.Contains(pr.Priority)) GUI.color = colOrig * GuiShadowedMult;

                var slidersRect = new Rect(
                    SlidersDistFromLeftBorder,
                    (SliderHeight + 3 * ButtonHeight) * row,
                    tableSizeX + SlidersDistFromRightBorder + SliderWidth,
                    SliderHeight + 3 * ButtonHeight + 5f);

                //draw bottom line
                Widgets.DrawLine(
                    new Vector2(slidersRect.xMin, slidersRect.yMax),
                    new Vector2(slidersRect.xMax, slidersRect.yMax),
                    new Color(0.7f, 0.7f, 0.7f),
                    1f);

                pr.Priority = DrawUtil.PriorityBox(
                    slidersRect.xMin,
                    slidersRect.yMin + slidersRect.height / 2f,
                    pr.Priority.v);
                workTables[table] = pr;

                var maxJobsElementXPos = slidersRect.xMin + SliderMargin;
                var maxJobsLabel = "Max jobs for pawn";

                var maxJobsLabelRect = new Rect(
                    maxJobsElementXPos - maxJobsLabel.GetWidthCached() / 2,
                    slidersRect.yMin + 20f,
                    120f,
                    LabelHeight);
                Widgets.Label(maxJobsLabelRect, maxJobsLabel);

                var maxJobsSliderRect = new Rect(maxJobsElementXPos, slidersRect.yMin + 60f, SliderWidth, SliderHeight);

                var newMaxJobsSliderValue = MaxJobsPerPawnSlider(
                    maxJobsSliderRect,
                    Mathf.Clamp(pr.JobCount.v, 0f, _pawnsData.WorkTypes.Count),
                    out var skipTextAssign);

                var jobCountMaxLabelRect = new Rect(
                    maxJobsSliderRect.xMax - PercentStringWidth,
                    maxJobsSliderRect.yMax + 3f,
                    PercentStringWidth,
                    25f);

                newMaxJobsSliderValue = MaxJobsPerPawnField(
                    jobCountMaxLabelRect,
                    newMaxJobsSliderValue,
                    skipTextAssign);

                pr.JobCount = Mathf.RoundToInt(newMaxJobsSliderValue);
                workTables[table] = pr;

                // draw line on the right from max job sliders
                Widgets.DrawLine(
                    new Vector2(maxJobsLabelRect.xMax, slidersRect.yMin),
                    new Vector2(maxJobsLabelRect.xMax, slidersRect.yMax),
                    new Color(0.5f, 0.5f, 0.5f),
                    1f);

                DrawWorkListForPriority(
                    pr,
                    new Rect(maxJobsLabelRect.xMax, slidersRect.y, slidersRect.width, slidersRect.height));

                _prioritiesEncounteredCached.Add(pr.Priority);
                //return to normal
                GUI.color = colOrig;
            }

            Widgets.EndScrollView();

            var removePriorityButtonRect = new Rect(
                inRect.xMax - SliderMargin,
                scrollRect.yMax + 9f,
                ButtonHeight,
                ButtonHeight);
            if (Widgets.ButtonImage(removePriorityButtonRect, Resources.MinusIcon))
            {
                RemovePriority();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }

            var addPriorityButtonRect = new Rect(
                removePriorityButtonRect.xMin - StandardMargin - removePriorityButtonRect.width,
                scrollRect.yMax + 9f,
                ButtonHeight,
                ButtonHeight);
            if (Widgets.ButtonImage(addPriorityButtonRect, Resources.PlusIcon))
            {
                AddPriority();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private float MaxJobsPerPawnSlider(Rect rect, float currentMaxJobs, out bool valueWasAssigned)
        {
            var newMaxJobsSliderValue = GUI.VerticalSlider(rect, currentMaxJobs, _pawnsData.WorkTypes.Count, 0f);
            valueWasAssigned = Math.Abs(newMaxJobsSliderValue - currentMaxJobs) > 0.0001;
            return newMaxJobsSliderValue;
        }

        private float MaxJobsPerPawnField(Rect jobCountMaxLabelRect, float newMaxJobsSliderValue, bool skipAssign)
        {
            var maxJobsText = _textFieldBuffers.GetValueOrDefault(jobCountMaxLabelRect) ?? Mathf
                .RoundToInt(newMaxJobsSliderValue)
                .ToString();
            var prevSliderVal = newMaxJobsSliderValue;
            Widgets.TextFieldNumeric(jobCountMaxLabelRect, ref newMaxJobsSliderValue, ref maxJobsText);

            // so that the text input doesn't override values set by slider
            if (skipAssign)
            {
                _textFieldBuffers[jobCountMaxLabelRect] = null;
                return prevSliderVal;
            }

            if (Math.Abs(prevSliderVal - newMaxJobsSliderValue) < 0.0001)
                _textFieldBuffers[jobCountMaxLabelRect] = maxJobsText;
            else
                _textFieldBuffers[jobCountMaxLabelRect] = null;
            return newMaxJobsSliderValue;
        }

        private void DrawWorkListForPriority(WorkTableEntry pr, Rect slidersRect)
        {
            // using (_profilerFactory.CreateProfiler("DrawWorkListForPriority"))
            {
                foreach (var (i, workType) in _pawnsData.WorkTypes.Select((x, i) => (i, x)))
                    // using (_profilerFactory.CreateProfiler("DrawWorkListForPriority inner loop"))
                {
                    var workName = workType.LabelShort;
                    try
                    {
                        var currentPercent = pr.WorkTypes[workType];

                        var numberColonists = _pawnsData.NumberColonists(workType);
                        float elementXPos;
                        Rect labelRect;
                        double available;
                        bool takenMoreThanTotal;
                        // using (_profilerFactory.CreateProfiler("DrawWorkListForPriority PercentColonistsAvailable"))
                        {
                            var (available1, takenMoreThanTotal1) =
                                _pawnsData.PercentColonistsAvailable(workType, pr.Priority);
                            available = available1;
                            takenMoreThanTotal = takenMoreThanTotal1;
                            elementXPos = slidersRect.x + SliderMargin / 2 + SliderMargin * i;
                            labelRect = new Rect(
                                elementXPos - workName.GetWidthCached() / 2,
                                slidersRect.yMin + (i % 2 == 0 ? 0f : 20f) + 10f,
                                100f,
                                LabelHeight);

                            WorkTypeLabel(takenMoreThanTotal, labelRect, workName);
                        }

                        float currSliderVal;
                        Rect sliderRect;
                        bool skipNextAssign;
                        // using (_profilerFactory.CreateProfiler("DrawWorkListForPriority SliderPercentsInput"))
                        {
                            sliderRect = new Rect(elementXPos, slidersRect.yMin + 60f, SliderWidth, SliderHeight);
                            currSliderVal = (float)currentPercent.Value;

                            currSliderVal = SliderPercentsInput(
                                sliderRect,
                                (float)available,
                                currSliderVal,
                                out skipNextAssign);
                        }

                        Rect percentsRect;
                        // using (_profilerFactory.CreateProfiler("DrawWorkListForPriority TextPercentsInput"))
                        {
                            percentsRect = new Rect(
                                sliderRect.xMax - PercentStringWidth,
                                sliderRect.yMax + 3f,
                                PercentStringWidth,
                                25f);

                            currSliderVal = TextPercentsInput(
                                percentsRect,
                                currentPercent,
                                currSliderVal,
                                takenMoreThanTotal,
                                (float)available,
                                skipNextAssign,
                                numberColonists);
                        }

                        // using (_profilerFactory.CreateProfiler(
                        //     "DrawWorkListForPriority SwitchPercentsNumbersButton"))
                        {
                            var switchRect = new Rect(
                                percentsRect.min + new Vector2(5f + PercentStringLabelWidth, 0f),
                                percentsRect.size);
                            var symbolRect = new Rect(switchRect.min + new Vector2(5f, 0f), switchRect.size);
                            pr.WorkTypes[workType] = SwitchPercentsNumbersButton(
                                symbolRect,
                                currentPercent,
                                numberColonists,
                                currSliderVal);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Err($"Error for work type {workName}:");
                        _logger.Err(e);
                    }
                }
            }
        }

        private static void WorkTypeLabel(bool takenMoreThanTotal, Rect rect, string workName)
        {
            var prevCol = GUI.color;
            if (takenMoreThanTotal) GUI.color = Color.red;

            Widgets.Label(rect, workName);

            GUI.color = prevCol;
        }

        private float SliderPercentsInput(Rect sliderRect,
            float available,
            float currSliderVal,
            out bool skipNextAssign)
        {
            var newSliderValue = GUI.VerticalSlider(sliderRect, currSliderVal, Math.Max(1f, currSliderVal), 0f);

            skipNextAssign = Math.Abs(newSliderValue - currSliderVal) > 0.0001;

            return Mathf.Clamp(newSliderValue, 0f, Math.Max(available, currSliderVal));
        }

        private float TextPercentsInput(Rect rect,
            TablePercent currentPercent,
            float currentValue,
            bool takenMoreThanTotal,
            float available,
            bool skipAssign,
            int totalColonists)
        {
            var value = Mathf.Round(
                currentPercent.Variant switch
                {
                    PercentVariant.Percent => currentValue * 100f,
                    PercentVariant.Number => currentValue * totalColonists,
                    _ => throw new ArgumentOutOfRangeException(nameof(currentPercent), currentPercent, null)
                });

            var percentsText = _textFieldBuffers.GetValueOrDefault(rect) ?? Mathf.RoundToInt(value)
                .ToString(CultureInfo.InvariantCulture);

            var prevCol = GUI.color;
            if (takenMoreThanTotal) GUI.color = Color.red;

            var prevSliderVal = value;
            Widgets.TextFieldNumeric(rect, ref value, ref percentsText);
            if (skipAssign)
            {
                _textFieldBuffers[rect] = null;
                return currentValue;
            }

            if (Math.Abs(prevSliderVal - value) < 0.0001)
                _textFieldBuffers[rect] = percentsText;
            else
                _textFieldBuffers[rect] = null;

            GUI.color = prevCol;
            var currSliderVal = currentPercent.Variant switch
            {
                PercentVariant.Percent => value / 100f,
                PercentVariant.Number => value / totalColonists,
                _ => throw new ArgumentOutOfRangeException(nameof(currentPercent), currentPercent, null)
            };
            return Mathf.Clamp(currSliderVal, 0f, Math.Max(available, currentValue));
        }

        private TablePercent SwitchPercentsNumbersButton(Rect rect,
            TablePercent currentPercent,
            int numberColonists,
            float sliderValue)
        {
            switch (currentPercent.Variant)
            {
                case PercentVariant.Number:
                    if (Widgets.ButtonText(rect, "№"))
                    {
                        // clear buffers so that text input uses new values
                        _textFieldBuffers.Clear();

                        currentPercent = TablePercent.Percent(sliderValue);
                    }
                    else
                    {
                        currentPercent = TablePercent.Number(
                            numberColonists,
                            Mathf.RoundToInt(sliderValue * numberColonists));
                    }

                    break;
                case PercentVariant.Percent:
                    if (Widgets.ButtonText(rect, "%"))
                    {
                        // clear buffers so that text input uses new values
                        _textFieldBuffers.Clear();

                        currentPercent = TablePercent.Number(
                            numberColonists,
                            Mathf.RoundToInt(sliderValue * numberColonists));
                    }
                    else
                    {
                        currentPercent = TablePercent.Percent(sliderValue);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return currentPercent;
        }

        private void PawnExcludeTab(Rect inRect)
        {
            const float fromTopToTickboxesVertical = WorkLabelOffset + LabelHeight + 15f;

            var scrollRect = new Rect(inRect.xMin, inRect.yMin, inRect.width, inRect.height - DistFromBottomBorder);

            var tableSizeX = PawnNameCoWidth + WorkLabelWidth / 2 + WorkLabelHorizOffset * _pawnsData.WorkTypes.Count;

            var tableSizeY = fromTopToTickboxesVertical
                             + (LabelMargin + ButtonHeight) * _pawnsData.AllPlayerPawns.Count;
            Widgets.BeginScrollView(scrollRect, ref _pawnExcludeScrollPos, new Rect(0, 0, tableSizeX, tableSizeY));

            var tickboxesRect = new Rect(
                PawnNameCoWidth,
                fromTopToTickboxesVertical,
                tableSizeX - PawnNameCoWidth,
                tableSizeY - fromTopToTickboxesVertical);
            var anchor = Text.Anchor;
#if DEBUG
            //Widgets.DrawBox(tickboxesRect); 
#endif
            // draw worktypes
            Text.Anchor = TextAnchor.UpperCenter;
            foreach (var (workType, i) in _pawnsData.WorkTypes.Zip(
                Enumerable.Range(0, _pawnsData.WorkTypes.Count),
                (w, i) => (w, i)))
            {
                var workLabel = workType.LabelShort;
                var rect = new Rect(
                    tickboxesRect.xMin + WorkLabelHorizOffset * i,
                    i % 2 == 0 ? 0f : WorkLabelOffset,
                    WorkLabelWidth,
                    LabelHeight);
                Widgets.Label(rect, workLabel);
#if DEBUG
                //Widgets.DrawBox(rect);
#endif
                var horizLinePos = rect.center.x;
                Widgets.DrawLine(
                    new Vector2(horizLinePos, rect.yMax),
                    new Vector2(horizLinePos, tickboxesRect.yMin),
                    Color.grey,
                    1f);
            }

            Text.Anchor = TextAnchor.UpperLeft;
            foreach (var (pawn, rowi) in _pawnsData.AllPlayerPawns.Select((w, i) => (w, i)))
            {
                // draw pawn name
                var nameRect = new Rect(
                    0f,
                    tickboxesRect.yMin + (LabelMargin + ButtonHeight) * rowi,
                    PawnNameCoWidth,
                    LabelHeight + LabelMargin);
                Widgets.Label(nameRect, pawn.LabelNoCount);
                TooltipHandler.TipRegion(nameRect, "Click here to toggle all jobs");
                if (Widgets.ButtonInvisible(nameRect))
                {
                    var c = _pawnsData.ExcludedPawns.Count(x => x.PawnThingId == pawn.ThingID);
                    if (c > _pawnsData.WorkTypes.Count / 2)
                        _pawnsData.ExcludedPawns.RemoveWhere(x => x.PawnThingId == pawn.ThingID);
                    else
                        foreach (var work in _pawnsData.WorkTypes)
                            _pawnsData.ExcludedPawns.Add(
                                new ExcludedPawnEntry { WorkDef = work.DefName, PawnThingId = pawn.ThingID });
                }

                Widgets.DrawLine(
                    new Vector2(nameRect.xMin, nameRect.yMax),
                    new Vector2(tickboxesRect.xMax, nameRect.yMax),
                    Color.grey,
                    1f);

                // draw tickboxes
                foreach (var (workType, i) in _pawnsData.WorkTypes.Zip(
                    Enumerable.Range(0, _pawnsData.WorkTypes.Count),
                    (w, i) => (w, i)))
                {
                    var prev = _pawnsData.ExcludedPawns.Contains(
                        new ExcludedPawnEntry { WorkDef = workType.DefName, PawnThingId = pawn.ThingID });
                    var next = prev;
                    DrawUtil.EmptyCheckbox(
                        nameRect.xMax - (ButtonHeight - 1) / 2 + (i + 1) * WorkLabelHorizOffset,
                        nameRect.yMin,
                        ref next);
                    if (prev == next) continue;

                    if (next)
                    {
                        _pawnsData.ExcludedPawns.Add(
                            new ExcludedPawnEntry { WorkDef = workType.DefName, PawnThingId = pawn.ThingID });
#if DEBUG
                        _logger.Info(
                            $"Pawn {pawn.NameFullColored} with work {workType.DefName} was added to the Excluded list");
#endif
                    }
                    else
                    {
                        _pawnsData.ExcludedPawns.Remove(
                            new ExcludedPawnEntry { WorkDef = workType.DefName, PawnThingId = pawn.ThingID });
#if DEBUG
                        _logger.Info(
                            $"Pawn {pawn.NameFullColored} with work {workType.DefName} was removed from the Excluded list");
#endif
                    }
                }
            }

            Widgets.EndScrollView();

            Text.Anchor = anchor;
        }

        private void AddPriority()
        {
            var dict = new Dictionary<IWorkTypeWrapper, TablePercent>();
            _pawnsData.WorkTables.Add(
                new WorkTableEntry { Priority = 0, JobCount = _pawnsData.WorkTypes.Count, WorkTypes = dict });

            foreach (var keyValue in _pawnsData.WorkTypes) dict.Add(keyValue, TablePercent.Percent(0));
        }

        private void RemovePriority()
        {
            if (_pawnsData.WorkTables.Count > 0) _pawnsData.WorkTables.RemoveLast();
        }

        private enum SelectedTab
        {
            Priorities = 1,
            PawnExclusion = 2,
            ImportantWorkTypes = 3
        }
    }
}
