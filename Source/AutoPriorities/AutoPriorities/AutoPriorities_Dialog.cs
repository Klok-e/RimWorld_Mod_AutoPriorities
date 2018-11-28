using AutoPriorities.Core;
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
        private static List<Tuple2<int, Dictionary<WorkTypeDef, float>>> _priorityToWorkTypesAndPercentOfPawns;
        private static HashSet<WorkTypeDef> _workTypes;
        private static HashSet<WorkTypeDef> _workTypesNotRequiringSkills;
        private static int _pawnsCountForDeterminingWhetherToRebuild;
        private static Dictionary<WorkTypeDef, List<Tuple2<Pawn, float>>> _sortedPawnSkillForEveryWork;
        private static HashSet<Pawn> _allPlayerPawns;

        private static HashSet<int> _prioritiesEncounteredCached;

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

        static AutoPriorities_Dialog()
        {
            _allPlayerPawns = new HashSet<Pawn>();
            _prioritiesEncounteredCached = new HashSet<int>();
            _workTypes = new HashSet<WorkTypeDef>();
            _workTypesNotRequiringSkills = new HashSet<WorkTypeDef>();
            _pawnsCountForDeterminingWhetherToRebuild = -1;
            _sortedPawnSkillForEveryWork = new Dictionary<WorkTypeDef, List<Tuple2<Pawn, float>>>();

            _priorityToWorkTypesAndPercentOfPawns = new List<Tuple2<int, Dictionary<WorkTypeDef, float>>>();
            LoadSavedState();
        }

        public AutoPriorities_Dialog() : base()
        {
            doCloseButton = true;
            draggable = true;
            resizeable = true;
            _scrollPos = new Vector2();
        }

        private static void LoadSavedState()
        {
            RebuildIfDirtySortedPawnSkillForEveryWork();
            try
            {
                _priorityToWorkTypesAndPercentOfPawns = PercentPerWorkTypeSaver.LoadState();

                //check whether state is correct
                bool correct = true;
                foreach(var keyVal in _priorityToWorkTypesAndPercentOfPawns)
                {
                    foreach(var work in _workTypes)
                    {
                        if(!keyVal._val2.ContainsKey(work))
                        {
                            Log.Message($"AutoPriorities: {work.labelShort} has been found but was not present in a save file");
                            correct = false;
                            goto outOfCycles;
                        }
                    }
                }
                outOfCycles:
                if(!correct)
                {
                    _priorityToWorkTypesAndPercentOfPawns = new List<Tuple2<int, Dictionary<WorkTypeDef, float>>>();
                    Log.Message("AutoPriorities: Priorities have been reset.");
                }
            }
            catch(System.IO.FileNotFoundException)
            {
                _priorityToWorkTypesAndPercentOfPawns = new List<Tuple2<int, Dictionary<WorkTypeDef, float>>>();
            }
            catch(Exception e)
            {
                Log.Error(e.Message);
                _priorityToWorkTypesAndPercentOfPawns = new List<Tuple2<int, Dictionary<WorkTypeDef, float>>>();
            }
        }

        private static void SaveState()
        {
            try
            {
                PercentPerWorkTypeSaver.SaveState(_priorityToWorkTypesAndPercentOfPawns);
            }
            catch(Exception e)
            {
                Log.Error(e.Message);
            }
        }

        public override void DoWindowContents(Rect windowRect)
        {
            RebuildIfDirtySortedPawnSkillForEveryWork();

            var worktypes = _sortedPawnSkillForEveryWork.Count;

            var scrollRect = new Rect(windowRect.xMin, windowRect.yMin, windowRect.width, windowRect.height - _distFromBottomBorder);

            var tableSizeX = (worktypes + 1) * _sliderMargin + _slidersDistFromLeftBorder + _slidersDistFromRightBorder;
            var scrollWidth = tableSizeX > windowRect.width ? tableSizeX : windowRect.width;

            var tableSizeY = (_sliderHeight + 3 * _buttonHeight) * _priorityToWorkTypesAndPercentOfPawns.Count;
            var scrollHeight = tableSizeY > scrollRect.height ? tableSizeY : windowRect.height;
            Widgets.BeginScrollView(scrollRect, ref _scrollPos, new Rect(0, 0, scrollWidth, scrollHeight));

            _prioritiesEncounteredCached.Clear();
            int row = 0;
            foreach(var pr in _priorityToWorkTypesAndPercentOfPawns)
            {
                var colOrig = GUI.color;
                //shadow repeating priorities
                if(_prioritiesEncounteredCached.Contains(pr._val1))
                    GUI.color = colOrig * _guiShadowedMult;

                var slidersRect = new Rect(_slidersDistFromLeftBorder, (_sliderHeight + 3 * _buttonHeight) * row, tableSizeX + _slidersDistFromRightBorder, _sliderHeight + 3 * _buttonHeight);

                //draw bottom line
                Widgets.DrawLine(new Vector2(slidersRect.xMin, slidersRect.yMax), new Vector2(slidersRect.xMax, slidersRect.yMax), new Color(0.7f, 0.7f, 0.7f), 1f);

                var priorityButtonRect = new Rect(slidersRect.xMin, slidersRect.yMin + (slidersRect.height / 2f), _buttonHeight, _buttonHeight);

                pr._val1 = DrawUtil.PriorityBox(slidersRect.xMin, slidersRect.yMin + (slidersRect.height / 2f), pr._val1);

                int i = 0;
                foreach(var workType in _workTypes)
                {
                    var workName = workType.labelShort;
                    try
                    {
                        float elementXPos = slidersRect.xMin + _sliderMargin * (i + 1);

                        var labelRect = new Rect(elementXPos - (workName.GetWidthCached() / 2), slidersRect.yMin + (i % 2 == 0 ? 0f : 30f), 100f, 30f);
                        Widgets.Label(labelRect, workName);

                        var sliderRect = new Rect(elementXPos, slidersRect.yMin + 60f, _sliderWidth, _sliderHeight);
                        var newSliderValue = GUI.VerticalSlider(sliderRect, pr._val2[workType], 1f, 0f);
                        var available = PercentOfColonistsAvailable(workType, pr._val1);
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
                _prioritiesEncounteredCached.Add(pr._val1);
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
                AssignPriorities();
                SaveState();
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

        private static void AddPriority()
        {
            var dict = new Dictionary<WorkTypeDef, float>();
            _priorityToWorkTypesAndPercentOfPawns.Add(new Tuple2<int, Dictionary<WorkTypeDef, float>>(0, dict));

            foreach(var keyValue in _workTypes)
                dict.Add(keyValue, 0f);
        }

        private static void RemovePriority()
        {
            if(_priorityToWorkTypesAndPercentOfPawns.Count > 0)
                _priorityToWorkTypesAndPercentOfPawns.RemoveLast();
        }

        private static float PercentOfColonistsAvailable(WorkTypeDef workType, int priorityIgnore)
        {
            float taken = 0;
            foreach(var tuple in _priorityToWorkTypesAndPercentOfPawns)
            {
                if(tuple._val1 == priorityIgnore)
                    continue;
                taken += tuple._val2[workType];
                if(taken > 1f)
                    Log.Error($"Percent of colonists assigned to work type {workType.defName} is greater than 1: {taken}");
            }
            return 1f - taken;
        }

        private static void AssignPriorities()
        {
            var listOfPawnAndAmountOfJobsAssigned = new Dictionary<Pawn, int>(_allPlayerPawns.Count);
            foreach(var item in _allPlayerPawns)
                listOfPawnAndAmountOfJobsAssigned.Add(item, 0);

            var priorityToPercentOfColonists = new List<Tuple2<int, float>>();
            foreach(var work in _workTypes)
            {
                //skip works not requiring skills because they will be handled later
                if(_workTypesNotRequiringSkills.Contains(work))
                    continue;

                FillListOfPriorityToPercentOfColonists(work, priorityToPercentOfColonists);

                var pawns = _sortedPawnSkillForEveryWork[work];
                float pawnsCount = pawns.Count;

                _prioritiesEncounteredCached.Clear();
                int pawnsIterated = 0;
                float mustBeIteratedForThisPriority = 0f;
                foreach(var priorityToPercent in priorityToPercentOfColonists)
                {
                    //skip repeating priorities
                    if(_prioritiesEncounteredCached.Contains(priorityToPercent._val1))
                        continue;

                    mustBeIteratedForThisPriority += priorityToPercent._val2 * pawnsCount;
                    //Log.Message($"mustBeIteratedForThisPriority {priorityToPercent._val1}: {mustBeIteratedForThisPriority}; pawnsCount: {pawnsCount}");
                    for(; pawnsIterated < mustBeIteratedForThisPriority; pawnsIterated++)
                    {
                        var currentPawn = pawns[pawnsIterated];

                        //skip incapable pawns
                        if(currentPawn._val1.IsCapableOfWholeWorkType(work))
                        {
                            //Log.Message($"in loop mustBeIteratedForThisPriority {priorityToPercent._val1}: {mustBeIteratedForThisPriority}; pawnsIterated: {pawnsIterated}");
                            currentPawn._val1.workSettings.SetPriority(work, priorityToPercent._val1);

                            listOfPawnAndAmountOfJobsAssigned[currentPawn._val1] += 1;
                        }
                    }
                    _prioritiesEncounteredCached.Add(priorityToPercent._val1);
                }
                //set remaining pawns to 0
                if(pawnsIterated < pawnsCount)
                {
                    for(; pawnsIterated < pawnsCount; pawnsIterated++)
                    {
                        if(!pawns[pawnsIterated]._val1.IsCapableOfWholeWorkType(work))
                            continue;
                        pawns[pawnsIterated]._val1.workSettings.SetPriority(work, 0);
                    }
                }
            }

            //turn dict to list
            var jobsCount = new List<Tuple2<Pawn, int>>(listOfPawnAndAmountOfJobsAssigned.Count);
            foreach(var item in listOfPawnAndAmountOfJobsAssigned)
                jobsCount.Add(new Tuple2<Pawn, int>(item.Key, item.Value));

            //sort by ascending to then iterate (lower count of works assigned gets works first)
            jobsCount.Sort((x, y) => x._val2.CompareTo(y._val2));
            foreach(var work in _workTypesNotRequiringSkills)
            {
                FillListOfPriorityToPercentOfColonists(work, priorityToPercentOfColonists);

                _prioritiesEncounteredCached.Clear();
                int pawnsIterated = 0;
                float mustBeIteratedForThisPriority = 0f;
                foreach(var priorityToPercent in priorityToPercentOfColonists)
                {
                    //skip repeating priorities
                    if(_prioritiesEncounteredCached.Contains(priorityToPercent._val1))
                        continue;

                    mustBeIteratedForThisPriority += priorityToPercent._val2 * jobsCount.Count;
                    for(; pawnsIterated < mustBeIteratedForThisPriority; pawnsIterated++)
                    {
                        var currentPawn = jobsCount[pawnsIterated];

                        //skip incapable pawns
                        if(currentPawn._val1.IsCapableOfWholeWorkType(work))
                            currentPawn._val1.workSettings.SetPriority(work, priorityToPercent._val1);
                    }
                    _prioritiesEncounteredCached.Add(priorityToPercent._val1);
                }
                //set remaining pawns to 0
                if(pawnsIterated < jobsCount.Count)
                {
                    for(; pawnsIterated < jobsCount.Count; pawnsIterated++)
                    {
                        if(!jobsCount[pawnsIterated]._val1.IsCapableOfWholeWorkType(work))
                            continue;
                        jobsCount[pawnsIterated]._val1.workSettings.SetPriority(work, 0);
                    }
                }
            }

            void FillListOfPriorityToPercentOfColonists(WorkTypeDef work, List<Tuple2<int, float>> toFill)
            {
                toFill.Clear();
                foreach(var priority in _priorityToWorkTypesAndPercentOfPawns)
                    toFill.Add(new Tuple2<int, float>(priority._val1, priority._val2[work]));
                toFill.Sort((x, y) => x._val1.CompareTo(y._val1));
            }
        }

        private static void RebuildIfDirtySortedPawnSkillForEveryWork()
        {
            if(_pawnsCountForDeterminingWhetherToRebuild == Find.CurrentMap.mapPawns.AllPawnsCount)
                return;

            // get all work types
            var workTypes = WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder;

            // get all pawns owned by player
            var pawns = Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer);
            _pawnsCountForDeterminingWhetherToRebuild = Find.CurrentMap.mapPawns.AllPawnsCount;

            // get all skills associated with the work types
            _allPlayerPawns.Clear();
            _sortedPawnSkillForEveryWork.Clear();
            foreach(var work in workTypes)
            {
                foreach(var pawn in pawns)
                {
                    if(pawn.AnimalOrWildMan())
                        continue;

                    if(!_allPlayerPawns.Contains(pawn))
                        _allPlayerPawns.Add(pawn);

                    float skill = 0;
                    try
                    {
                        skill = pawn.skills.AverageOfRelevantSkillsFor(work);
                    }
                    catch(Exception e)
                    {
                        Log.Message($"error: {e} for pawn {pawn.Name.ToStringFull}");
                    }
                    if(_sortedPawnSkillForEveryWork.ContainsKey(work))
                    {
                        _sortedPawnSkillForEveryWork[work].Add(new Tuple2<Pawn, float>(pawn, skill));
                    }
                    else
                    {
                        _sortedPawnSkillForEveryWork.Add(work, new List<Tuple2<Pawn, float>>
                        {
                            new Tuple2<Pawn, float>(pawn, skill),
                        });
                    }

                }
                if(!_workTypes.Contains(work))
                {
                    _workTypes.Add(work);
                    if(work.relevantSkills.Count == 0)
                        _workTypesNotRequiringSkills.Add(work);
                }
            }

            foreach(var keyValue in _sortedPawnSkillForEveryWork)
            {
                keyValue.Value.Sort((x, y) => y._val2.CompareTo(x._val2));
            }
        }
    }
}
