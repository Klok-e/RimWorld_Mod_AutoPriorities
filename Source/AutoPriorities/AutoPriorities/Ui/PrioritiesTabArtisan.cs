using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoPriorities.Core;
using AutoPriorities.Percents;
using AutoPriorities.Utils;
using AutoPriorities.WorldInfoRetriever;
using AutoPriorities.Wrappers;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using ILogger = AutoPriorities.APLogger.ILogger;
using Resources = AutoPriorities.Core.Resources;

namespace AutoPriorities.Ui
{
    public class PrioritiesTabArtisan
    {
        private readonly ILogger _logger;

        private readonly PawnsData _pawnsData;
        private readonly HashSet<Priority> _prioritiesEncounteredCached = new();
        private readonly IWorldInfoRetriever _worldInfoRetriever;
        private Rect _currViewedScrollRect;
        private Vector2 _scrollPos;

        public PrioritiesTabArtisan(PawnsData pawnsData, ILogger logger, IWorldInfoRetriever worldInfoRetriever)
        {
            _pawnsData = pawnsData;
            _logger = logger;
            _worldInfoRetriever = worldInfoRetriever;
        }

        public void PrioritiesTab(Rect inRect)
        {
            // using (_profilerFactory.CreateProfiler("PrioritiesTab"))
            var workTypes = _pawnsData.SortedPawnFitnessForEveryWork.Count;

            var scrollRect = new Rect(inRect.xMin, inRect.yMin, inRect.width, inRect.height - Consts.DistFromBottomBorder);

            var tableSizeX = (workTypes + 1) * Consts.SliderMargin
                             + Consts.SlidersDistFromLeftBorder
                             + Consts.SlidersDistFromRightBorder
                             + Consts.SliderMargin;

            var tableSizeY = (Consts.SliderHeight + 3 * Consts.ButtonHeight) * _pawnsData.WorkTables.Count;
            Widgets.BeginScrollView(scrollRect, ref _scrollPos, new Rect(0, 0, tableSizeX, tableSizeY));

            _currViewedScrollRect = new Rect(_scrollPos.x, _scrollPos.y, scrollRect.width, scrollRect.height);

            _prioritiesEncounteredCached.Clear();
            var workTables = _pawnsData.WorkTables;
            for (int table = 0, row = 0; table < workTables.Count; table++, row++)
            {
                // table row
                var pr = workTables[table];
                var colOrig = GUI.color;

                //shadow repeating priorities
                if (_prioritiesEncounteredCached.Contains(pr.Priority)) GUI.color = colOrig * Consts.GuiShadowedMult;

                var slidersRect = new Rect(
                    Consts.SlidersDistFromLeftBorder,
                    (Consts.SliderHeight + 3 * Consts.ButtonHeight) * row,
                    tableSizeX + Consts.SlidersDistFromRightBorder + Consts.SliderWidth,
                    Consts.SliderHeight + 3 * Consts.ButtonHeight + 5f
                );

                //draw bottom line
                Widgets.DrawLine(
                    new Vector2(slidersRect.xMin, slidersRect.yMax),
                    new Vector2(slidersRect.xMax, slidersRect.yMax),
                    new Color(0.7f, 0.7f, 0.7f),
                    1f
                );

                pr.Priority = DrawUtil.PriorityBox(
                    slidersRect.xMin,
                    slidersRect.yMin + slidersRect.height / 2f,
                    pr.Priority.v,
                    _worldInfoRetriever.GetMaxPriority()
                );
                workTables[table] = pr;

                var maxJobsElementXPos = slidersRect.xMin + Consts.SliderMargin;
                var maxJobsLabel = "Max jobs for pawn";

                var maxJobsLabelRect = new Rect(
                    maxJobsElementXPos - maxJobsLabel.GetWidthCached() / 2,
                    slidersRect.yMin + 20f,
                    120f,
                    Consts.LabelHeight
                );
                Widgets.Label(maxJobsLabelRect, maxJobsLabel);

                var maxJobsSliderRect = new Rect(maxJobsElementXPos, slidersRect.yMin + 60f, Consts.SliderWidth, Consts.SliderHeight);

                var newMaxJobsSliderValue = MaxJobsPerPawnSlider(
                    maxJobsSliderRect,
                    Mathf.Clamp(pr.JobCount.v, 0f, _pawnsData.WorkTypes.Count),
                    out var skipTextAssign
                );

                var jobCountMaxLabelRect = new Rect(
                    maxJobsSliderRect.xMax - Consts.PercentStringWidth,
                    maxJobsSliderRect.yMax + 3f,
                    Consts.PercentStringWidth,
                    25f
                );

                newMaxJobsSliderValue = MaxJobsPerPawnField(jobCountMaxLabelRect, newMaxJobsSliderValue, skipTextAssign);

                pr.JobCount = Mathf.RoundToInt(newMaxJobsSliderValue);
                workTables[table] = pr;

                // draw line on the right from max job sliders
                Widgets.DrawLine(
                    new Vector2(maxJobsLabelRect.xMax, slidersRect.yMin),
                    new Vector2(maxJobsLabelRect.xMax, slidersRect.yMax),
                    new Color(0.5f, 0.5f, 0.5f),
                    1f
                );

                DrawWorkListForPriority(pr, new Rect(maxJobsLabelRect.xMax, slidersRect.y, slidersRect.width, slidersRect.height));

                _prioritiesEncounteredCached.Add(pr.Priority);
                //return to normal
                GUI.color = colOrig;
            }

            Widgets.EndScrollView();

            var removePriorityButtonRect = new Rect(
                inRect.xMax - Consts.SliderMargin,
                scrollRect.yMax + 9f,
                Consts.ButtonHeight,
                Consts.ButtonHeight
            );
            if (Widgets.ButtonImage(removePriorityButtonRect, Resources.MinusIcon))
            {
                RemovePriority();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }

            var addPriorityButtonRect = new Rect(
                removePriorityButtonRect.xMin - Window.StandardMargin - removePriorityButtonRect.width,
                scrollRect.yMax + 9f,
                Consts.ButtonHeight,
                Consts.ButtonHeight
            );
            if (Widgets.ButtonImage(addPriorityButtonRect, Resources.PlusIcon))
            {
                AddPriority();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private float MaxJobsPerPawnSlider(Rect rect, float currentMaxJobs, out bool wasValueChanged)
        {
            if (!rect.Overlaps(_currViewedScrollRect))
            {
                wasValueChanged = false;
                return currentMaxJobs;
            }

            var newMaxJobsSliderValue = GUI.VerticalSlider(rect, currentMaxJobs, _pawnsData.WorkTypes.Count, 0f);
            wasValueChanged = Math.Abs(newMaxJobsSliderValue - currentMaxJobs) > 0.0001;
            return newMaxJobsSliderValue;
        }

        private float MaxJobsPerPawnField(Rect jobCountMaxLabelRect, float newMaxJobsSliderValue, bool skipAssign)
        {
            if (!jobCountMaxLabelRect.Overlaps(_currViewedScrollRect)) return newMaxJobsSliderValue;

            var maxJobsText = Mathf.RoundToInt(newMaxJobsSliderValue).ToString();
            var prevSliderVal = newMaxJobsSliderValue;
            Widgets.TextFieldNumeric(jobCountMaxLabelRect, ref newMaxJobsSliderValue, ref maxJobsText);

            // so that the text input doesn't override values set by slider
            if (skipAssign) return prevSliderVal;

            return newMaxJobsSliderValue;
        }

        private void DrawWorkListForPriority(WorkTableEntry pr, Rect slidersRect)
        {
            // using (_profilerFactory.CreateProfiler("DrawWorkListForPriority"))
            foreach (var (i, workType) in _pawnsData.WorkTypes.Select((x, i) => (i, x)))
                // using (_profilerFactory.CreateProfiler("DrawWorkListForPriority inner loop"))
            {
                var workName = workType.LabelShort;
                try
                {
                    var currentPercent = pr.WorkTypes[workType];
                    var numberColonists = _pawnsData.NumberColonists(workType);
                    var remainPercentExistsForWorkType = _pawnsData.PercentRemainExistsForWorkType(workType);

#if DEBUG
                    var isCallInteresting = false;
#endif

                    float elementXPos;
                    Rect labelRect;
                    double available;
                    bool takenMoreThanTotal;
                    // using (_profilerFactory.CreateProfiler("DrawWorkListForPriority PercentColonistsAvailable"))
                    {
                        var (available1, takenMoreThanTotal1) = _pawnsData.PercentColonistsAvailable(workType, pr.Priority);
                        available = available1;
                        takenMoreThanTotal = takenMoreThanTotal1;
                        elementXPos = slidersRect.x + Consts.SliderMargin / 2 + Consts.SliderMargin * i;
                        labelRect = new Rect(
                            elementXPos - workName.GetWidthCached() / 2,
                            slidersRect.yMin + (i % 2 == 0 ? 0f : 20f) + 10f,
                            100f,
                            Consts.LabelHeight
                        );

                        WorkTypeLabel(takenMoreThanTotal, labelRect, workName);
                    }

                    float currSliderVal;
                    Rect sliderRect;
                    bool didValueChange;
                    // using (_profilerFactory.CreateProfiler("DrawWorkListForPriority SliderPercentsInput"))
                    {
                        sliderRect = new Rect(elementXPos, slidersRect.yMin + 60f, Consts.SliderWidth, Consts.SliderHeight);
                        currSliderVal = (float)_pawnsData.PercentValue(currentPercent, workType, pr.Priority);

#if DEBUG
                        var prevSliderVal = currSliderVal;
#endif

                        currSliderVal = SliderPercentsInput(sliderRect, (float)available, currSliderVal, out didValueChange);
#if DEBUG
                        if (Math.Abs(currSliderVal - prevSliderVal) > 0.001)
                        {
                            _logger.Info($"slider: available {available}; prevSliderVal {prevSliderVal}; currSliderVal {currSliderVal}");
                            isCallInteresting = true;
                        }
#endif
                    }

                    Rect percentsRect;
                    // using (_profilerFactory.CreateProfiler("DrawWorkListForPriority TextPercentsInput"))
                    {
                        percentsRect = new Rect(
                            sliderRect.xMax - Consts.PercentStringWidth,
                            sliderRect.yMax + 3f,
                            Consts.PercentStringWidth,
                            25f
                        );

#if DEBUG
                        var prevSliderVal = currSliderVal;
#endif
                        currSliderVal = TextPercentsInput(
                            percentsRect,
                            currentPercent,
                            currSliderVal,
                            takenMoreThanTotal,
                            (float)available,
                            didValueChange,
                            numberColonists
                        );
#if DEBUG
                        if (Math.Abs(currSliderVal - prevSliderVal) > 0.001)
                        {
                            _logger.Info($"text: available {available}; prevSliderVal {prevSliderVal}; currSliderVal {currSliderVal}");
                            isCallInteresting = true;
                        }
#endif
                    }

                    // using (_profilerFactory.CreateProfiler(
                    //     "DrawWorkListForPriority SwitchPercentsNumbersButton"))
                    {
                        var switchRect = new Rect(
                            percentsRect.min + new Vector2(5f + Consts.PercentStringLabelWidth, 0f),
                            percentsRect.size
                        );
                        var symbolRect = new Rect(switchRect.min + new Vector2(5f, 0f), switchRect.size);
                        var switchPercentsNumbersButton = SwitchPercentsNumbersButton(
                            symbolRect,
                            currentPercent,
                            numberColonists,
                            currSliderVal,
                            remainPercentExistsForWorkType
                        );
#if DEBUG
                        if (isCallInteresting)
                            _logger.Info(
                                $"button {switchPercentsNumbersButton.variant.ToString()}; "
                                + $"percent {_pawnsData.PercentValue(switchPercentsNumbersButton, workType, pr.Priority)}"
                            );
#endif
                        pr.WorkTypes[workType] = switchPercentsNumbersButton;
                    }
                }
                catch (Exception e)
                {
                    _logger.Err($"Error for work type {workName}:");
                    _logger.Err(e);
                }
            }
        }

        private void WorkTypeLabel(bool takenMoreThanTotal, Rect rect, string workName)
        {
            if (!rect.Overlaps(_currViewedScrollRect)) return;

            var prevCol = GUI.color;
            if (takenMoreThanTotal) GUI.color = Color.red;

            Widgets.Label(rect, workName);

            GUI.color = prevCol;
        }

        private float SliderPercentsInput(Rect sliderRect, float available, float currSliderVal, out bool didValueChange)
        {
            if (!sliderRect.Overlaps(_currViewedScrollRect))
            {
                didValueChange = false;
                return currSliderVal;
            }

            var newSliderValue = GUI.VerticalSlider(sliderRect, currSliderVal, Math.Max(1f, currSliderVal), 0f);

            didValueChange = Math.Abs(newSliderValue - currSliderVal) > 0.0001;

            return Mathf.Clamp(newSliderValue, 0f, Math.Max(available, currSliderVal));
        }

        private float TextPercentsInput(Rect rect, TablePercent currentPercent, float currentValue, bool takenMoreThanTotal,
            float available, bool skipAssign, int totalColonists)
        {
            if (!rect.Overlaps(_currViewedScrollRect)) return currentValue;

            var value = Mathf.Round(
                currentPercent.variant switch
                {
                    PercentVariant.Percent => currentValue * 100f,
                    PercentVariant.PercentRemaining => available * 100f,
                    PercentVariant.Number => currentValue * totalColonists,
                    _ => throw new ArgumentOutOfRangeException(nameof(currentPercent), currentPercent, null),
                }
            );

            var percentsText = Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);

            var prevCol = GUI.color;
            if (takenMoreThanTotal) GUI.color = Color.red;

            Widgets.TextFieldNumeric(rect, ref value, ref percentsText);

            if (skipAssign) return currentValue;

            GUI.color = prevCol;
            var currSliderVal = currentPercent.variant switch
            {
                PercentVariant.Percent => value / 100f,
                PercentVariant.PercentRemaining => value / 100f,
                PercentVariant.Number => value / totalColonists,
                _ => throw new ArgumentOutOfRangeException(nameof(currentPercent), currentPercent, null),
            };
            return Mathf.Clamp(currSliderVal, 0f, Math.Max(available, currentValue));
        }

        private TablePercent SwitchPercentsNumbersButton(Rect rect, TablePercent currentPercent, int numberColonists, float sliderValue,
            bool remainPercentExistsForWorkType)
        {
            if (!rect.Overlaps(_currViewedScrollRect)) return currentPercent;

            var switchButtonResult = currentPercent.variant switch
            {
                PercentVariant.Percent => Widgets.ButtonText(rect, "%") ? PercentVariant.Number : PercentVariant.Percent,
                PercentVariant.Number => Widgets.ButtonText(rect, "№")
                    ? remainPercentExistsForWorkType ? PercentVariant.Percent : PercentVariant.PercentRemaining
                    : PercentVariant.Number,
                PercentVariant.PercentRemaining => Widgets.ButtonText(rect, "R") ? PercentVariant.Percent : PercentVariant.PercentRemaining,
                _ => throw new ArgumentOutOfRangeException(),
            };

            currentPercent = switchButtonResult switch
            {
                PercentVariant.Number => TablePercent.Number(Mathf.RoundToInt(sliderValue * numberColonists)),
                PercentVariant.Percent => TablePercent.Percent(sliderValue),
                PercentVariant.PercentRemaining => TablePercent.PercentRemaining(),
                _ => throw new ArgumentOutOfRangeException(),
            };

            return currentPercent;
        }


        private void AddPriority()
        {
            var dict = new Dictionary<IWorkTypeWrapper, TablePercent>();
            _pawnsData.WorkTables.Add(new WorkTableEntry { Priority = 0, JobCount = _pawnsData.WorkTypes.Count, WorkTypes = dict });

            foreach (var keyValue in _pawnsData.WorkTypes) dict.Add(keyValue, TablePercent.Percent(0));
        }

        private void RemovePriority()
        {
            if (_pawnsData.WorkTables.Count > 0) _pawnsData.WorkTables.RemoveLast();
        }
    }
}
