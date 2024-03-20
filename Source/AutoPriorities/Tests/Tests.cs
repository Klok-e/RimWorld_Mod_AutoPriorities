using System;
using System.Linq;
using AutoPriorities.Extensions;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class Tests
    {
        private static object[] _testIterPercentsSource =
        {
            new object[] { new[] { (0, 0), (1, 1), (2, 1), (3, 2), (4, 2), (5, 2) }, new[] { 0.1, 0.2, 0.3 }, 10 },
            new object[] { new[] { (0, 0), (1, 0) }, new[] { 0.2d }, 10 },
            new object[]
            {
                new[] { (0, 0), (1, 0), (2, 0), (3, 0), (4, 1), (5, 1), (6, 1), (7, 1), (8, 1), (9, 2) },
                new[] { 0.4, 0.5, 0.3 }, 10
            }
        };

        [Test]
        [TestCaseSource(nameof(_testIterPercentsSource))]
        public void TestIterPercents((int, int)[] expected, double[] percents, int total)
        {
            // act
            var actual = percents.IterPercents(total)
                                 .ToArray();

            // assert
            Assert.That(expected, Is.EqualTo(actual));
        }

        [Test]
        public void TestIterPercentsCoverage()
        {
            for (var i = 0; i < 100; i++)
            {
                var percents = new[] { i / 100d, (100 - i) / 100d };
                for (var k = 0; k < 100; k++)
                {
                    var arr = percents.IterPercents(k)
                                      .ToArray();
                    Assert.That(k, Is.EqualTo(arr.Length));
                }
            }

            var p = new[] { new[] { 0.3 }, new[] { 0.1, 0.5 }, new[] { 0.2, 0.7 }, new[] { 0.4, 0.05 }, new[] { 0.5 } };
            foreach (var arr in p)
                for (var i = 0; i < 100; i++)
                {
                    var covered = (int)Math.Ceiling(i * arr.Sum());
                    var actual = arr.IterPercents(i)
                                    .ToArray();
                    Assert.That(covered, Is.EqualTo(actual.Length));
                }
        }

        [Test]
        public void TestDistinct()
        {
            var v = new[] { (1, 2d), (4, 1d), (2, 1d), (7, 1d), (8, 1d), (9, 2d), (1, 1d) };

            var actual = v.Distinct(x => x.Item1)
                          .ToArray();

            Assert.That(new[] { (1, 2d), (4, 1d), (2, 1d), (7, 1d), (8, 1d), (9, 2d) }, Is.EqualTo(actual));
        }
    }
}
