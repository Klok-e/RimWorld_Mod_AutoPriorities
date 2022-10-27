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
        private readonly float _importantJobsLabelWidth = Consts.ImportantJobsLabel.GetWidthCached() + 10f;
        private readonly IImportantJobsProvider _importantJobsProvider;
        private readonly float _importExportImportLabelWidth = Consts.DeleteLabel.GetWidthCached() + 10f;
        private readonly float _labelWidth = Consts.Label.GetWidthCached() + 10f;
        private readonly ILogger _logger;
        private readonly IPawnDataExporter _pawnDataExporter;
        private readonly float _pawnExcludeLabelWidth = Consts.PawnExcludeLabel.GetWidthCached() + 10f;
        private readonly PawnsData _pawnsData;
        private readonly PrioritiesAssigner _prioritiesAssigner;

        private readonly float _prioritiesLabelWidth = Consts.PrioritiesLabel.GetWidthCached() + 10f;
        private SelectedTab _currentlySelectedTab = SelectedTab.Priorities;
        private bool _openedOnce;
        private Vector2 _pawnExcludeScrollPos;
        private Rect _rect;

        private PrioritiesTabArtisan _prioritiesTabArtisan;

        private readonly QuickProfilerFactory _profilerFactory = new();
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
            _prioritiesTabArtisan = new PrioritiesTabArtisan(_pawnsData, _logger);
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
            // using var p = _profilerFactory.CreateProfiler("DoWindowContents");

            // draw select tab buttons
            var prioritiesButtonRect = new Rect(inRect.xMin, inRect.yMin, _prioritiesLabelWidth, Consts.LabelHeight);
            if (Widgets.ButtonText(prioritiesButtonRect, Consts.PrioritiesLabel))
            {
                _currentlySelectedTab = SelectedTab.Priorities;
                _pawnsData.Rebuild();
            }

            var pawnsButtonRect = new Rect(
                prioritiesButtonRect.xMax + 5f,
                prioritiesButtonRect.yMin,
                _pawnExcludeLabelWidth,
                Consts.LabelHeight);
            if (Widgets.ButtonText(pawnsButtonRect, Consts.PawnExcludeLabel))
            {
                _currentlySelectedTab = SelectedTab.PawnExclusion;
                _pawnsData.Rebuild();
            }

            var importantButtonRect = new Rect(
                pawnsButtonRect.xMax + 5f,
                prioritiesButtonRect.yMin,
                _importantJobsLabelWidth,
                Consts.LabelHeight);
            if (Widgets.ButtonText(importantButtonRect, Consts.ImportantJobsLabel))
            {
                _currentlySelectedTab = SelectedTab.ImportantWorkTypes;
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
                    ImportantWorkTypes(lowerInRect);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_currentlySelectedTab));
            }

            var buttonDeleteRect = new Rect(
                inRect.xMax - _importExportImportLabelWidth,
                inRect.yMin,
                _importExportImportLabelWidth,
                Consts.LabelHeight);
            DrawDeleteButton(buttonDeleteRect);

            var buttonImportRect = new Rect(
                buttonDeleteRect.xMin - _importExportImportLabelWidth,
                inRect.yMin,
                _importExportImportLabelWidth,
                Consts.LabelHeight);
            DrawImportButton(buttonImportRect);

            var buttonExportRect = new Rect(
                buttonImportRect.xMin - _importExportImportLabelWidth,
                inRect.yMin,
                _importExportImportLabelWidth,
                Consts.LabelHeight);
            DrawExportButton(buttonExportRect);

            var buttonRunRect = new Rect(
                inRect.xMin,
                inRect.yMax - Consts.ButtonHeight,
                _labelWidth,
                Consts.ButtonHeight);
            DrawRunButton(buttonRunRect);

            // if (_windowContentsCalls % 1000 == 0) _profilerFactory.SaveProfileData();

            // _windowContentsCalls += 1;
        }

        private void DrawRunButton(Rect inRect)
        {
            if (Widgets.ButtonText(inRect, Consts.Label))
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
            if (saves.Any() && Widgets.ButtonText(inRect, Consts.ImportLabel))
            {
                var options = saves.Select(
                        x => new FloatMenuOption(
                            x,
                            () => { _pawnDataExporter.ImportPawnData(x); }))
                    .ToList();
                Find.WindowStack.Add(new FloatMenu(options, string.Empty));
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private void DrawExportButton(Rect inRect)
        {
            if (Widgets.ButtonText(inRect, Consts.ExportLabel))
            {
                Find.WindowStack.Add(new NameExportDialog(_pawnDataExporter));
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private void DrawDeleteButton(Rect inRect)
        {
            var saves = _pawnDataExporter.ListSaves()
                .ToList();
            if (saves.Any() && Widgets.ButtonText(inRect, Consts.DeleteLabel))
            {
                var options = saves.Select(x => new FloatMenuOption(x, () => _pawnDataExporter.DeleteSave(x)))
                    .ToList();
                Find.WindowStack.Add(new FloatMenu(options, string.Empty));
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private void ImportantWorkTypes(Rect inRect)
        {
            const float fromTopToTickboxesVertical = Consts.WorkLabelOffset + Consts.LabelHeight + 15f;

            var scrollRect = new Rect(
                inRect.xMin,
                inRect.yMin,
                inRect.width,
                inRect.height - Consts.DistFromBottomBorder);

            var tableSizeX = Consts.WorkLabelWidth / 2 +
                             Consts.WorkLabelHorizOffset * _pawnsData.WorkTypesNotRequiringSkills.Count;

            var tableSizeY = fromTopToTickboxesVertical + (Consts.LabelMargin + Consts.ButtonHeight);
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
                    tickboxesRect.xMin + Consts.WorkLabelHorizOffset * i,
                    i % 2 == 0 ? 0f : Consts.WorkLabelOffset,
                    Consts.WorkLabelWidth,
                    Consts.LabelHeight);
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
                    (Consts.ButtonHeight - 1) / 2 + i * Consts.WorkLabelHorizOffset + 11f,
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

        private void PawnExcludeTab(Rect inRect)
        {
            const float fromTopToTickboxesVertical = Consts.WorkLabelOffset + Consts.LabelHeight + 15f;

            var scrollRect = new Rect(
                inRect.xMin,
                inRect.yMin,
                inRect.width,
                inRect.height - Consts.DistFromBottomBorder);

            var tableSizeX = Consts.PawnNameCoWidth + Consts.WorkLabelWidth / 2 +
                             Consts.WorkLabelHorizOffset * _pawnsData.WorkTypes.Count;

            var tableSizeY = fromTopToTickboxesVertical
                             + (Consts.LabelMargin + Consts.ButtonHeight) * _pawnsData.CurrentMapPlayerPawns.Count;
            Widgets.BeginScrollView(scrollRect, ref _pawnExcludeScrollPos, new Rect(0, 0, tableSizeX, tableSizeY));

            var tickboxesRect = new Rect(
                Consts.PawnNameCoWidth,
                fromTopToTickboxesVertical,
                tableSizeX - Consts.PawnNameCoWidth,
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
                    tickboxesRect.xMin + Consts.WorkLabelHorizOffset * i,
                    i % 2 == 0 ? 0f : Consts.WorkLabelOffset,
                    Consts.WorkLabelWidth,
                    Consts.LabelHeight);
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
            foreach (var (pawn, rowi) in _pawnsData.CurrentMapPlayerPawns.Select((w, i) => (w, i)))
            {
                // draw pawn name
                var nameRect = new Rect(
                    0f,
                    tickboxesRect.yMin + (Consts.LabelMargin + Consts.ButtonHeight) * rowi,
                    Consts.PawnNameCoWidth,
                    Consts.LabelHeight + Consts.LabelMargin);
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
                        nameRect.xMax - (Consts.ButtonHeight - 1) / 2 + (i + 1) * Consts.WorkLabelHorizOffset,
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

        private enum SelectedTab
        {
            Priorities = 1,
            PawnExclusion = 2,
            ImportantWorkTypes = 3
        }
    }
}
