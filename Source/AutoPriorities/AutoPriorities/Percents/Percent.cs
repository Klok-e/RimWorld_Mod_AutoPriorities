namespace AutoPriorities.Percents
{
    public class Percent : IPercent
    {
        public Variant Variant => Variant.Percent;
        public float Value { get; }

        public Percent(float value)
        {
            Value = value;
        }
    }
}