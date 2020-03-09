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
            var percents = new List<double> {0.1, 0.2, 0.3};
            var expected = new[] {(0, 0), (1, 1), (2, 1), (3, 2), (4, 2), (5, 2)};

            Assert.AreEqual(expected, percents.IterPercents(10).ToArray());
        }

        [Test]
        public void TestIterPercentsCoverage()
        {
            var percents = new List<double> {0.3, 0.2, 0.5};

            for (int i = 0; i < 1000; i++)
            {
                var arr = percents.IterPercents(i).ToArray();
                Assert.AreEqual(i, arr.Length);
            }
        }
    }
}