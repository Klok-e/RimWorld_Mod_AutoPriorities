using AutoPriorities.Extensions;
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
        private static List<Tuple2<int, Dictionary<WorkTypeDef, float>>> _priorityToDictOfWorkTypeToPercentOfColonists;
        private static HashSet<WorkTypeDef> _workTypes;
        private static int _pawnsCountForDeterminingWhetherToRebuild;
        private static Dictionary<WorkTypeDef, List<Tuple2<Pawn, float>>> _sortedPawnSkillForEveryWork;

        private const float _sliderWidth = 20f;
        private const float _sliderHeight = 60f;
        private const float _sliderMargin = 80f;

        private const float _slidersDistFromLeftBorder = 30f;
        private const float _slidersDistFromRightBorder = 30f;
        private const float _distFromTopBorder = 80f;
        private const float _distFromBottomBorder = 50f;

        private const float _scrollSize = 30f;

        private const float _buttonHeight = 30f;

        private Vector2 _scrollPos;

        static AutoPriorities_Dialog()
        {
            _workTypes = new HashSet<WorkTypeDef>();
            _priorityToDictOfWorkTypeToPercentOfColonists = new List<Tuple2<int, Dictionary<WorkTypeDef, float>>>();
            _pawnsCountForDeterminingWhetherToRebuild = 0;
            _sortedPawnSkillForEveryWork = new Dictionary<WorkTypeDef, List<Tuple2<Pawn, float>>>();
        }

        public AutoPriorities_Dialog() : base()
        {
            doCloseButton = true;
            draggable = true;
            resizeable = true;
            _scrollPos = new Vector2();
        }

        public override void DoWindowContents(Rect windowRect)
        {
            RebuildIfDirtySortedPawnSkillForEveryWork();

            var worktypes = _sortedPawnSkillForEveryWork.Count;

            var scrollRect = new Rect(windowRect.xMin, windowRect.yMin, windowRect.width, windowRect.height - _distFromBottomBorder);

            var tableSizeX = (worktypes + 1) * _sliderMargin + _slidersDistFromLeftBorder + _slidersDistFromRightBorder;
            var scrollWidth = tableSizeX > windowRect.width ? tableSizeX : windowRect.width;

            var tableSizeY = (_sliderHeight + 3 * _buttonHeight) * _priorityToDictOfWorkTypeToPercentOfColonists.Count;
            var scrollHeight = tableSizeY > scrollRect.height ? tableSizeY : windowRect.height;
            Widgets.BeginScrollView(scrollRect, ref _scrollPos, new Rect(0, 0, scrollWidth, scrollHeight));

            int row = 0;
            foreach(var pr in _priorityToDictOfWorkTypeToPercentOfColonists)
            {
                var slidersRect = new Rect(_slidersDistFromLeftBorder, (_sliderHeight + 3 * _buttonHeight) * row, tableSizeX + _slidersDistFromRightBorder, _sliderHeight + 3 * _buttonHeight);

                //draw bottom line
                Widgets.DrawLine(new Vector2(slidersRect.xMin, slidersRect.yMax), new Vector2(slidersRect.xMax, slidersRect.yMax), new Color(0.7f, 0.7f, 0.7f), 1f);

                var priorityButtonRect = new Rect(slidersRect.xMin, slidersRect.yMin + (slidersRect.height / 2f), _buttonHeight, _buttonHeight);

                pr._val1 = DrawUtil.PriorityBox(slidersRect.xMin, slidersRect.yMin + (slidersRect.height / 2f), pr._val1);

                int i = 0;
                foreach(var workType in _sortedPawnSkillForEveryWork.Keys)
                {
                    var workName = workType.defName;

                    float elementXPos = slidersRect.xMin + _sliderMargin * (i + 1);

                    var labelRect = new Rect(elementXPos - (workName.GetWidthCached() / 2), slidersRect.yMin + (i % 2 == 0 ? 0f : 30f), 100f, 30f);
                    Widgets.Label(labelRect, workName);

                    var sliderRect = new Rect(elementXPos, slidersRect.yMin + 60f, _sliderWidth, _sliderHeight);
                    pr._val2[workType] = GUI.VerticalSlider(sliderRect, pr._val2[workType], 1, 0);

                    var percentLabelText = Mathf.RoundToInt(pr._val2[workType] * 100f).ToString() + "%";
                    var percentLabelRect = new Rect(
                        sliderRect.xMax - (_sliderWidth / 2) - (percentLabelText.GetWidthCached() / 2),
                        sliderRect.yMax,
                        percentLabelText.GetWidthCached(),
                        25f);
                    Widgets.Label(percentLabelRect, percentLabelText);

                    i += 1;
                }
                row += 1;
            }
            Widgets.EndScrollView();

            var label = "Run AutoPriorities";
            var buttonRect = new Rect(
                windowRect.xMin,
                scrollRect.yMax + 9f,
                label.GetWidthCached() + 10f,
                _buttonHeight);
            if(Widgets.ButtonText(buttonRect, label))
                foreach(var tuple in _priorityToDictOfWorkTypeToPercentOfColonists)
                    AssignPriorities(tuple);

            var removePriorityButtonRect = new Rect(
                windowRect.xMax - _sliderMargin,
                scrollRect.yMax + 9f,
                _buttonHeight,
                _buttonHeight);
            if(Widgets.ButtonImage(removePriorityButtonRect, Core.Resources._minusIcon))
                RemovePriority();

            var addPriorityButtonRect = new Rect(
                removePriorityButtonRect.xMin - StandardMargin - removePriorityButtonRect.width,
                scrollRect.yMax + 9f,
                _buttonHeight,
                _buttonHeight);
            if(Widgets.ButtonImage(addPriorityButtonRect, Core.Resources._plusIcon))
                AddPriority();
        }

        private void AddPriority()
        {
            var dict = new Dictionary<WorkTypeDef, float>();
            _priorityToDictOfWorkTypeToPercentOfColonists.Add(
                new Tuple2<int, Dictionary<WorkTypeDef, float>>(4, dict));

            foreach(var keyValue in _workTypes)
                dict.Add(keyValue, 0f);

            SoundDefOf.AmountIncrement.PlayOneShotOnCamera();
        }

        private void RemovePriority()
        {
            if(_priorityToDictOfWorkTypeToPercentOfColonists.Count > 0)
                _priorityToDictOfWorkTypeToPercentOfColonists.RemoveLast();
            SoundDefOf.AmountIncrement.PlayOneShotOnCamera();
        }

        private void AssignPriorities(Tuple2<int, Dictionary<WorkTypeDef, float>> priorityWorks)
        {
            foreach(var keyValue in _sortedPawnSkillForEveryWork)
            {
                float pawnsIterated = 1;
                float pawnsCount = keyValue.Value.Count;
                foreach(var tuple in keyValue.Value)
                {
                    if(!tuple._val1.IsCapableOfWholeWorkType(keyValue.Key))
                        continue;

                    // if percent iterated is greater than specified
                    if(pawnsIterated / pawnsCount > priorityWorks._val2[keyValue.Key])
                    {
                        tuple._val1.workSettings.Disable(keyValue.Key);
                        //Log.Message($"broke for {tuple._val1.Name.ToStringFull} work {keyValue.Key.defName}");
                    }
                    else
                    {
                        tuple._val1.workSettings.SetPriority(keyValue.Key, priorityWorks._val1);
                    }

                    pawnsIterated += 1;
                }
            }
            SoundDefOf.AmountIncrement.PlayOneShotOnCamera();
        }

        private void RebuildIfDirtySortedPawnSkillForEveryWork()
        {
            if(_pawnsCountForDeterminingWhetherToRebuild == Find.CurrentMap.mapPawns.AllPawnsCount)
                return;

            // get all work types
            var workTypes = WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder;

            // get all pawns owned by player
            var pawns = Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer);
            _pawnsCountForDeterminingWhetherToRebuild = Find.CurrentMap.mapPawns.AllPawnsCount;

            // get all skills associated with the work types
            var workTypeSkillForEveryPawn = new Dictionary<WorkTypeDef, List<Tuple2<Pawn, float>>>();
            foreach(var work in workTypes)
            {
                foreach(var pawn in pawns)
                {
                    if(pawn.AnimalOrWildMan())
                        continue;
                    float skill = 0;
                    try
                    {
                        skill = pawn.skills.AverageOfRelevantSkillsFor(work);
                    }
                    catch(Exception e)
                    {
                        Log.Message($"error: {e} for pawn {pawn.Name.ToStringFull}");
                    }
                    if(workTypeSkillForEveryPawn.ContainsKey(work))
                    {
                        workTypeSkillForEveryPawn[work].Add(new Tuple2<Pawn, float>(pawn, skill));
                    }
                    else
                    {
                        workTypeSkillForEveryPawn.Add(work, new List<Tuple2<Pawn, float>>
                        {
                            new Tuple2<Pawn, float>(pawn, skill),
                        });
                    }

                }
                if(!_workTypes.Contains(work))
                    _workTypes.Add(work);
            }

            foreach(var keyValue in workTypeSkillForEveryPawn)
            {
                keyValue.Value.Sort((x, y) => y._val2.CompareTo(x._val2));
            }
            _sortedPawnSkillForEveryWork = workTypeSkillForEveryPawn;
            return;
        }
    }
}
