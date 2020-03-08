using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using AutoPriorities.Extensions;

namespace Tests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void TestIterPercents()
        {
            var percents = new List<float> {0.1f, 0.2f, 0.3f};

            Assert.AreEqual(percents.IterPercents(10).ToArray(),
                new[] {(0, 0), (1, 1), (2, 1), (3, 2), (4, 2), (5, 2)});
        }
    }
}