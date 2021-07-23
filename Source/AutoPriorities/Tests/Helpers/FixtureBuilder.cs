using AutoFixture;

namespace Tests.Helpers
{
    public static class FixtureBuilder
    {
        public static IFixture Create()
        {
            return new Fixture().Customize(
                new AutoPopulatedNSubstitutePropertiesCustomization());
        }
    }
}
