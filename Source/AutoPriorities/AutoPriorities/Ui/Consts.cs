namespace AutoPriorities.Ui
{
    public static class Consts
    {
        public const float SliderWidth = 20f;
        public const float SliderHeight = 60f;
        public const float SliderMargin = 75f;
        public const float GuiShadowedMult = 0.5f;
        public const float SlidersDistFromLeftBorder = 30f;
        public const float SlidersDistFromRightBorder = 20f;
        public const float DistFromBottomBorder = 50f;
        public const float ButtonHeight = 30f;
        public const float LabelHeight = 22f;
        public const float LabelMargin = 5f;
        public const float WorkLabelWidth = 75f;
        public const float WorkLabelOffset = 25f;
        public const float WorkLabelHorizOffset = 40f;
        public const float CheckboxSize = 24f;
        public const float PercentStringWidth = 30f;
        public const float PercentStringLabelWidth = 20f;
        public const string PrioritiesLabel = "Priorities";
        public const string PawnExcludeLabel = "Exclude Colonists";
        public const string ImportantJobsLabel = "Important Jobs";
        public const string Label = "Set priorities";
        public const string LoadingOptimizing = "Optimizing";
        public const string LoadingDot = ".";
        public const string OptimizationFailedMessage = "Set priorities failed to find a solution which satisfies all requirements";

        public const string IgnoreLearningRate = "Ignore learning rate";

        public const string IgnoreLearningRateTooltip = "If true, learning rate won't be taken into account when assigning priorities.";

        public const string IgnoreOppositionToWork = "Ignore opposition to work";

        public const string IgnoreOppositionToWorkTooltip =
            "if true, pawns with ideoligions opposing a type of work, will get assigned that work type anyway.";

        public const string MinimumSkillLevel = "Minimum skill level";

        public const string MinimumSkillLevelTooltip =
            "Determines whether the pawn is eligible for the work type. If MinimumSkillLevel < skill, work type isn't assigned.";

        public const string Misc = "Misc";
        public const string DeleteLabel = "Delete";
        public const string ExportLabel = "Export";
        public const string ImportLabel = "Import";
        public const float PawnNameCoWidth = 150f;
    }
}
