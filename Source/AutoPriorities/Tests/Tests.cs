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

        [Test]
        public void TestDistinct()
        {
            var v = new[] {(1, 2d), (4, 1d), (2, 1d), (7, 1d), (8, 1d), (9, 2d), (1, 1d)};

            var actual = v.Distinct(x => x.Item1).ToArray();

            Assert.AreEqual(new[] {(1, 2d), (4, 1d), (2, 1d), (7, 1d), (8, 1d), (9, 2d)}, actual);
        }
    }
}