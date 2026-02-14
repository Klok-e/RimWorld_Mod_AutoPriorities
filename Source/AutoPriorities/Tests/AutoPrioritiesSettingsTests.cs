using System.Collections.Generic;
using AutoPriorities.Core;
using FluentAssertions;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class AutoPrioritiesSettingsTests
    {
        [Test]
        public void ClampValues_WhenNonAdultForbiddenListIsNull_SetsHandlingAndHunting()
        {
            var settings = new AutoPrioritiesSettings { nonAdultForbiddenWorkTypeDefNames = null };

            settings.ClampValues();

            settings.nonAdultForbiddenWorkTypeDefNames.Should().Equal("Handling", "Hunting");
        }

        [Test]
        public void ClampValues_WhenNonAdultForbiddenListIsEmpty_PreservesEmpty()
        {
            var settings = new AutoPrioritiesSettings { nonAdultForbiddenWorkTypeDefNames = new List<string>() };

            settings.ClampValues();

            settings.nonAdultForbiddenWorkTypeDefNames.Should().BeEmpty();
        }

        [Test]
        public void ClampValues_WhenListHasValues_OnlySanitizesWithoutInjectingDefaults()
        {
            var values = new List<string> { "Mining", " ", "mining" };
            values.Add(null!);

            var settings = new AutoPrioritiesSettings { nonAdultForbiddenWorkTypeDefNames = values };

            settings.ClampValues();

            settings.nonAdultForbiddenWorkTypeDefNames.Should().Equal("Mining");
        }
    }
}
