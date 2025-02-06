using System;
using System.Globalization;
using System.Linq;
using System.Text;
using AutoPriorities.ImportantJobs;
using AutoPriorities.PawnDataSerializer.Exporter;
using AutoPriorities.Utils;
using AutoPriorities.WorldInfoRetriever;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using ILogger = AutoPriorities.APLogger.ILogger;

namespace AutoPriorities.Ui
{
    public class AutoPrioritiesDialog : Window
    {
        private readonly float _importantJobsLabelWidth = Consts.ImportantJobsLabel.GetWidthCached() + 10f;

        private readonly IImportantJobsProvider _importantJobsProvider;
        private readonly float _importExportDeleteLabelWidth = Consts.DeleteLabel.GetWidthCached() + 10f;
        private readonly float _labelWidth = Consts.Label.GetWidthCached() + 10f;
        private readonly ILogger _logger;
        private readonly float _miscLabelWidth = Consts.Misc.GetWidthCached() + 10f;
        private readonly PawnDataExporter _pawnDataExporter;
        private readonly float _pawnExcludeLabelWidth = Consts.PawnExcludeLabel.GetWidthCached() + 10f;
        private readonly PawnsData _pawnsData;
        private readonly PrioritiesAssigner _prioritiesAssigner;
        private readonly float _prioritiesLabelWidth = Consts.PrioritiesLabel.GetWidthCached() + 10f;
        private readonly PrioritiesTabArtisan _prioritiesTabArtisan;
        private readonly QuickProfilerFactory _profilerFactory = new();
        private readonly IWorldInfoRetriever _worldInfoRetriever;

        private SelectedTab _currentlySelectedTab = SelectedTab.Priorities;
        private Vector2 _importantWorksScrollPos;
        private volatile bool _isRunPrioritiesLoading;
        private string? _minimumFitnessInputBuffer;
        private bool _openedOnce;
        private Vector2 _pawnExcludeScrollPosCenter;

        private Vector2 _pawnExcludeScrollPosLeft;
        private Vector2 _pawnExcludeScrollPosTop;

        private Rect _rect;
        // private int _windowContentsCalls;

        public AutoPrioritiesDialog(PawnsData pawnsData, PrioritiesAssigner prioritiesAssigner, ILogger logger,
            IImportantJobsProvider importantJobsProvider, PawnDataExporter pawnDataExporter, IWorldInfoRetriever worldInfoRetriever)
        {
            _pawnsData = pawnsData;
            _prioritiesAssigner = prioritiesAssigner;
            _logger = logger;
            _importantJobsProvider = importantJobsProvider;
            _pawnDataExporter = pawnDataExporter;
            _worldInfoRetriever = worldInfoRetriever;
            doCloseButton = true;
            draggable = true;
            resizeable = true;
            _prioritiesTabArtisan = new PrioritiesTabArtisan(_pawnsData, _logger, _worldInfoRetriever);
        }

        public override Vector2 InitialSize => new(
            _prioritiesLabelWidth
            + _pawnExcludeLabelWidth
            + _importantJobsLabelWidth
            + _miscLabelWidth
            + _importExportDeleteLabelWidth * 4
            + Consts.LabelMargin * 6
            + 50,
            500
        );

        public override void PostClose()
        {
            base.PostClose();
            _rect = windowRect;
            _pawnsData.SaveState();
        }

        public override void PostOpen()
        {
            base.PostOpen();
            _pawnsData.Rebuild();
            if (_openedOnce)
                windowRect = _rect;
            else
                _openedOnce = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // using var p = _profilerFactory.CreateProfiler("DoWindowContents");

            try
            {
                // draw select tab buttons
                var tabButtonsRect = new Rect(inRect.xMin, inRect.yMin, _prioritiesLabelWidth, Consts.LabelHeight);
                if (Widgets.ButtonText(tabButtonsRect, Consts.PrioritiesLabel))
                {
                    _currentlySelectedTab = SelectedTab.Priorities;
                    _pawnsData.Rebuild();
                }

                tabButtonsRect = new Rect(
                    tabButtonsRect.xMax + Consts.LabelMargin,
                    tabButtonsRect.yMin,
                    _pawnExcludeLabelWidth,
                    Consts.LabelHeight
                );
                if (Widgets.ButtonText(tabButtonsRect, Consts.PawnExcludeLabel))
                {
                    _currentlySelectedTab = SelectedTab.PawnExclusion;
                    _pawnsData.Rebuild();
                }

                if (_worldInfoRetriever.GetUseOldAssignmentAlgorithm())
                {
                    tabButtonsRect = new Rect(
                        tabButtonsRect.xMax + Consts.LabelMargin,
                        tabButtonsRect.yMin,
                        _importantJobsLabelWidth,
                        Consts.LabelHeight
                    );
                    if (Widgets.ButtonText(tabButtonsRect, Consts.ImportantJobsLabel))
                    {
                        _currentlySelectedTab = SelectedTab.ImportantWorkTypes;
                        _pawnsData.Rebuild();
                    }
                }

                tabButtonsRect = new Rect(
                    tabButtonsRect.xMax + Consts.LabelMargin,
                    tabButtonsRect.yMin,
                    _miscLabelWidth,
                    Consts.LabelHeight
                );
                if (Widgets.ButtonText(tabButtonsRect, Consts.Misc))
                {
                    _currentlySelectedTab = SelectedTab.Misc;
                    _pawnsData.Rebuild();
                }

                // draw tab contents lower than buttons
                var lowerInRect = inRect;
                lowerInRect.yMin += Consts.LabelHeight + 10f;

                // draw currently selected tab
                switch (_currentlySelectedTab)
                {
                    case SelectedTab.Priorities:
                        _prioritiesTabArtisan.PrioritiesTab(lowerInRect);
                        break;
                    case SelectedTab.PawnExclusion:
                        PawnExcludeTab(lowerInRect);
                        break;
                    case SelectedTab.ImportantWorkTypes:
                        ImportantWorkTypesTab(lowerInRect);
                        break;
                    case SelectedTab.Misc:
                        MiscTab(lowerInRect);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_currentlySelectedTab));
                }

                var buttonDeleteRect = new Rect(
                    inRect.xMax - _importExportDeleteLabelWidth,
                    inRect.yMin,
                    _importExportDeleteLabelWidth,
                    Consts.LabelHeight
                );
                DrawDeleteButton(buttonDeleteRect);

                var buttonImportRect = new Rect(
                    buttonDeleteRect.xMin - _importExportDeleteLabelWidth - Consts.LabelMargin,
                    inRect.yMin,
                    _importExportDeleteLabelWidth,
                    Consts.LabelHeight
                );
                DrawImportButton(buttonImportRect);

                var buttonExportRect = new Rect(
                    buttonImportRect.xMin - _importExportDeleteLabelWidth - Consts.LabelMargin,
                    inRect.yMin,
                    _importExportDeleteLabelWidth,
                    Consts.LabelHeight
                );
                DrawExportButton(buttonExportRect);

                var buttonRunRect = new Rect(inRect.xMin, inRect.yMax - Consts.ButtonHeight, _labelWidth, Consts.ButtonHeight);
                DrawRunButton(buttonRunRect);

                // if (_windowContentsCalls % 1000 == 0) _profilerFactory.SaveProfileData();

                // _windowContentsCalls += 1;
            }
            catch (Exception e)
            {
                _logger.Err(e);
            }
        }

        private void DrawRunButton(Rect inRect)
        {
            string priorities;
            if (_isRunPrioritiesLoading)
            {
                var dotCount = (int)(Time.realtimeSinceStartup * 2f) % 4;

                var builder = new StringBuilder(Consts.LoadingOptimizing);
                for (var i = 0; i < dotCount; i++) builder.Append(Consts.LoadingDot);

                priorities = builder.ToString();
            }
            else
                priorities = Consts.Label;

            if (Widgets.ButtonText(inRect, priorities, active: !_isRunPrioritiesLoading))
            {
                _pawnsData.Rebuild();

                if (_worldInfoRetriever.GetUseOldAssignmentAlgorithm())
                    _prioritiesAssigner.AssignPriorities();
                else
                {
                    _isRunPrioritiesLoading = true;
                    _prioritiesAssigner.StartOptimizationTaskOfAssignPriorities(
                        () => _isRunPrioritiesLoading = false,
                        () => { Messages.Message(Consts.OptimizationFailedMessage, MessageTypeDefOf.RejectInput, false); }
                    );
                }

                _pawnsData.SaveState();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private void MiscTab(Rect inRect)
        {
            var checkLrIgnoreRect = new Rect(inRect.xMin, inRect.yMin, inRect.width, Consts.ButtonHeight);
            DrawIgnoreLearningRateCheckbox(checkLrIgnoreRect);

            var minFitnessRect = new Rect(inRect.xMin, checkLrIgnoreRect.yMax + Consts.LabelMargin, inRect.width, Consts.ButtonHeight);
            DrawMinimumFitnessInput(minFitnessRect);

            var checkIgnoreOppositionToWorkRect = new Rect(
                inRect.xMin,
                minFitnessRect.yMax + Consts.LabelMargin,
                inRect.width,
                Consts.ButtonHeight
            );
            DrawIgnoreOppositionToWorkCheckbox(checkIgnoreOppositionToWorkRect);
        }

        private void DrawCheckbox(Rect inRect, string labelText, string tooltipText, ref bool value)
        {
            Widgets.DrawHighlightIfMouseover(inRect);
            TooltipHandler.TipRegion(inRect, tooltipText);

            var checkboxValue = value;

            var labelRect = new Rect(inRect.xMin, inRect.yMin, inRect.width / 2, inRect.height);
            Widgets.Label(labelRect, labelText);

            var color = GUI.color;
            GUI.color = Color.white;
            Widgets.CheckboxDraw(labelRect.xMax, labelRect.yMin, checkboxValue, false);
            GUI.color = color;

            if (Widgets.ButtonInvisible(new Rect(labelRect.xMax, labelRect.yMin, Consts.CheckboxSize, Consts.CheckboxSize)))
            {
                checkboxValue = !checkboxValue;
                if (checkboxValue)
                {
                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                }
                else
                {
                    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                }
            }

            value = checkboxValue;
        }

        private void DrawIgnoreLearningRateCheckbox(Rect inRect)
        {
            var pawnsDataIgnoreLearningRate = _pawnsData.IgnoreLearningRate;

            DrawCheckbox(inRect, Consts.IgnoreLearningRate, Consts.IgnoreLearningRateTooltip, ref pawnsDataIgnoreLearningRate);

            _pawnsData.IgnoreLearningRate = pawnsDataIgnoreLearningRate;
        }

        private void DrawIgnoreOppositionToWorkCheckbox(Rect inRect)
        {
            var pawnsDataIgnoreOppositionToWork = _pawnsData.IgnoreOppositionToWork;

            DrawCheckbox(inRect, Consts.IgnoreOppositionToWork, Consts.IgnoreOppositionToWorkTooltip, ref pawnsDataIgnoreOppositionToWork);

            _pawnsData.IgnoreOppositionToWork = pawnsDataIgnoreOppositionToWork;
        }

        private void DrawNumericInput(Rect inRect, string labelText, string tooltipText, ref float value, ref string? buffer)
        {
            Widgets.DrawHighlightIfMouseover(inRect);
            TooltipHandler.TipRegion(inRect, tooltipText);

            var tempValue = value;

            var labelRect = new Rect(inRect.xMin, inRect.yMin, inRect.width / 2, inRect.height);
            Widgets.Label(labelRect, labelText);

            Widgets.TextFieldNumeric(new Rect(labelRect.xMax, labelRect.yMin, inRect.width / 2, inRect.height), ref tempValue, ref buffer);

            value = tempValue;
        }

        private void DrawMinimumFitnessInput(Rect inRect)
        {
            var pawnsDataMinimumSkillLevel = _pawnsData.MinimumSkillLevel;

            if (_minimumFitnessInputBuffer != pawnsDataMinimumSkillLevel.ToString(CultureInfo.CurrentCulture))
            {
                _minimumFitnessInputBuffer = null;
            }

            DrawNumericInput(
                inRect,
                Consts.MinimumSkillLevel,
                Consts.MinimumSkillLevelTooltip,
                ref pawnsDataMinimumSkillLevel,
                ref _minimumFitnessInputBuffer
            );

            _pawnsData.MinimumSkillLevel = pawnsDataMinimumSkillLevel;
        }

        private void DrawImportButton(Rect inRect)
        {
            var saves = _pawnDataExporter.ListImportableSaves().ToList();
            if (saves.Any() && Widgets.ButtonText(inRect, Consts.ImportLabel))
            {
                var options = saves.Select(x => new FloatMenuOption(x.FileName, x.ImportPawnData)).ToList();
                Find.WindowStack.Add(new FloatMenu(options, string.Empty));
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private void DrawExportButton(Rect inRect)
        {
            if (Widgets.ButtonText(inRect, Consts.ExportLabel))
            {
                var invalidNames = _pawnDataExporter.ListImportableSaves().Select(x => x.FileName).ToArray();
                var savedPawnDataReference = _pawnDataExporter.ExportCurrentData();

                Find.WindowStack.Add(new NameExportDialog(savedPawnDataReference, invalidNames));
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private void DrawDeleteButton(Rect inRect)
        {
            var saves = _pawnDataExporter.ListDeletableSaves().ToList();
            if (saves.Any() && Widgets.ButtonText(inRect, Consts.DeleteLabel))
            {
                var options = saves.Select(x => new FloatMenuOption(x.FileName, () => _pawnDataExporter.DeleteSave(x.FileName))).ToList();
                Find.WindowStack.Add(new FloatMenu(options, string.Empty));
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private void ImportantWorkTypesTab(Rect inRect)
        {
            const float fromTopToTickboxesVertical = Consts.WorkLabelOffset + Consts.LabelHeight + 15f;

            var scrollRect = new Rect(inRect.xMin, inRect.yMin, inRect.width, inRect.height - Consts.DistFromBottomBorder);

            var tableSizeX = Consts.WorkLabelWidth / 2 + Consts.WorkLabelHorizOffset * _pawnsData.WorkTypesNotRequiringSkills.Count;

            var tableSizeY = fromTopToTickboxesVertical + (Consts.LabelMargin + Consts.ButtonHeight);
            Widgets.BeginScrollView(scrollRect, ref _importantWorksScrollPos, new Rect(0, 0, tableSizeX, tableSizeY));

            var tickboxesRect = new Rect(0, fromTopToTickboxesVertical, tableSizeX, tableSizeY - fromTopToTickboxesVertical);
            var anchor = Text.Anchor;

            // Widgets.DrawBox(tickboxesRect);

            var workTypes = _importantJobsProvider.ImportantWorkTypes();

            // draw worktypes
            Text.Anchor = TextAnchor.UpperCenter;
            foreach (var (workType, i) in _pawnsData.WorkTypesNotRequiringSkills.Select((w, i) => (w, i)))
            {
                var workLabel = workType.LabelShort;
                var rect = new Rect(
                    tickboxesRect.xMin + Consts.WorkLabelHorizOffset * i,
                    i % 2 == 0 ? 0f : Consts.WorkLabelOffset,
                    Consts.WorkLabelWidth,
                    Consts.LabelHeight
                );
                Widgets.Label(rect, workLabel);

                // Widgets.DrawBox(rect);

                var horizLinePos = rect.center.x;
                Widgets.DrawLine(new Vector2(horizLinePos, rect.yMax), new Vector2(horizLinePos, tickboxesRect.yMin), Color.grey, 1f);

                var prev = workTypes.Contains(workType);
                var next = prev;
                DrawUtil.EmptyCheckbox((Consts.ButtonHeight - 1) / 2 + i * Consts.WorkLabelHorizOffset + 11f, tickboxesRect.yMin, ref next);
                if (prev == next)
                    continue;

                if (next)
                    workTypes.Add(workType);
                else
                    workTypes.Remove(workType);

                _importantJobsProvider.SaveImportantWorkTypes(workTypes.Select(x => x.DefName));
            }

            Widgets.EndScrollView();

            Text.Anchor = anchor;
        }

        private void PawnExcludeTab(Rect inRect)
        {
            const float fromTopToTickboxesVertical = Consts.WorkLabelOffset + Consts.LabelHeight + 15f;

            var tableSizeX = Consts.WorkLabelWidth / 2 + Consts.WorkLabelHorizOffset * _pawnsData.WorkTypes.Count;

            var tableSizeY = (Consts.LabelMargin + Consts.ButtonHeight) * _pawnsData.CurrentMapPlayerPawns.Count
                             + Consts.CheckboxHeight / 2;

            var anchor = Text.Anchor;
            Text.Anchor = TextAnchor.UpperLeft;

            var pawnsScrollRect = new Rect(
                inRect.xMin,
                inRect.yMin + fromTopToTickboxesVertical,
                Consts.PawnNameCoWidth,
                inRect.height - Consts.DistFromBottomBorder - fromTopToTickboxesVertical
            );

            Widgets.BeginScrollView(
                pawnsScrollRect,
                ref _pawnExcludeScrollPosLeft,
                new Rect(0, 0, Consts.PawnNameCoWidth, tableSizeY),
                false
            );

            var pawnNameRect = new Rect(0, 0, Consts.PawnNameCoWidth, Consts.LabelHeight + Consts.LabelMargin);

            foreach (var pawn in _pawnsData.CurrentMapPlayerPawns)
            {
                Widgets.Label(pawnNameRect, pawn.LabelNoCount);
                TooltipHandler.TipRegion(pawnNameRect, "Click here to toggle all jobs");
                if (Widgets.ButtonInvisible(pawnNameRect))
                {
                    var c = _pawnsData.ExcludedPawns.Count(x => x.Pawn == pawn);
                    if (c > _pawnsData.WorkTypes.Count / 2)
                        _pawnsData.ExcludedPawns.RemoveWhere(x => x.Pawn == pawn);
                    else
                    {
                        foreach (var work in _pawnsData.WorkTypes)
                            _pawnsData.ExcludedPawns.Add(new ExcludedPawnEntry { WorkDef = work, Pawn = pawn });
                    }
                }

                pawnNameRect.y = pawnNameRect.yMin + Consts.LabelMargin + Consts.ButtonHeight;
            }

            Text.Anchor = anchor;

            Widgets.EndScrollView();

            _pawnExcludeScrollPosCenter.y = _pawnExcludeScrollPosLeft.y;

            var workTypesScrollRect = new Rect(
                pawnsScrollRect.xMax,
                inRect.yMin,
                inRect.width - pawnsScrollRect.width,
                fromTopToTickboxesVertical
            );

            Widgets.BeginScrollView(
                workTypesScrollRect,
                ref _pawnExcludeScrollPosTop,
                new Rect(0, 0, tableSizeX, fromTopToTickboxesVertical),
                false
            );

            anchor = Text.Anchor;

            // draw worktypes
            Text.Anchor = TextAnchor.UpperCenter;
            foreach (var (workType, i) in _pawnsData.WorkTypes.Zip(Enumerable.Range(0, _pawnsData.WorkTypes.Count), (w, i) => (w, i)))
            {
                var workLabel = workType.LabelShort;
                var rect = new Rect(
                    Consts.WorkLabelHorizOffset * i,
                    i % 2 == 0 ? 0f : Consts.WorkLabelOffset,
                    Consts.WorkLabelWidth,
                    Consts.LabelHeight
                );
                Widgets.Label(rect, workLabel);

                var horizLinePos = rect.center.x;
                Widgets.DrawLine(
                    new Vector2(horizLinePos, rect.yMax),
                    new Vector2(horizLinePos, fromTopToTickboxesVertical),
                    Color.grey,
                    1f
                );
            }

            Widgets.EndScrollView();

            _pawnExcludeScrollPosCenter.x = _pawnExcludeScrollPosTop.x;

            var checkboxesScrollRect = new Rect(
                pawnsScrollRect.xMax,
                workTypesScrollRect.yMax,
                workTypesScrollRect.width,
                inRect.height - Consts.DistFromBottomBorder - workTypesScrollRect.height
            );

            Widgets.BeginScrollView(checkboxesScrollRect, ref _pawnExcludeScrollPosCenter, new Rect(0, 0, tableSizeX, tableSizeY));

            Text.Anchor = TextAnchor.UpperLeft;
            foreach (var (pawn, rowi) in _pawnsData.CurrentMapPlayerPawns.Select((w, i) => (w, i)))
            {
                var checkboxesRect = new Rect(
                    0f,
                    (Consts.LabelMargin + Consts.ButtonHeight) * rowi,
                    Consts.WorkLabelWidth / 2 + Consts.WorkLabelHorizOffset * _pawnsData.WorkTypes.Count,
                    Consts.LabelHeight + Consts.LabelMargin
                );

                Widgets.DrawLine(
                    new Vector2(checkboxesRect.xMin, checkboxesRect.yMax),
                    new Vector2(checkboxesRect.xMax, checkboxesRect.yMax),
                    Color.grey,
                    1f
                );

                // draw tickboxes
                foreach (var (workType, i) in _pawnsData.WorkTypes.Zip(Enumerable.Range(0, _pawnsData.WorkTypes.Count), (w, i) => (w, i)))
                {
                    var prev = _pawnsData.ExcludedPawns.Contains(new ExcludedPawnEntry { WorkDef = workType, Pawn = pawn });
                    var next = prev;

                    DrawUtil.EmptyCheckbox(
                        Consts.WorkLabelHorizOffset * i + Consts.WorkLabelWidth / 2 - Consts.CheckboxHeight / 2,
                        checkboxesRect.yMin,
                        ref next
                    );
                    if (prev == next)
                        continue;

                    if (next)
                    {
                        _pawnsData.ExcludedPawns.Add(new ExcludedPawnEntry { WorkDef = workType, Pawn = pawn });
                        if (_worldInfoRetriever.DebugLogs())
                            _logger.Info($"Pawn {pawn.NameFullColored} with work {workType.DefName} was added to the Excluded list");
                    }
                    else
                    {
                        _pawnsData.ExcludedPawns.Remove(new ExcludedPawnEntry { WorkDef = workType, Pawn = pawn });
                        if (_worldInfoRetriever.DebugLogs())
                            _logger.Info($"Pawn {pawn.NameFullColored} with work {workType.DefName} was removed from the Excluded list");
                    }
                }
            }

            Widgets.EndScrollView();

            _pawnExcludeScrollPosLeft.y = _pawnExcludeScrollPosCenter.y;
            _pawnExcludeScrollPosTop.x = _pawnExcludeScrollPosCenter.x;

            Text.Anchor = anchor;
        }

        private enum SelectedTab
        {
            Priorities = 1,
            PawnExclusion = 2,
            ImportantWorkTypes = 3,
            Misc = 4,
        }
    }
}
