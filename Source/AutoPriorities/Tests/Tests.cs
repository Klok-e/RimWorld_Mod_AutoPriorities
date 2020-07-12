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
            var e1 = new[] {(0, 0), (1, 1), (2, 1), (3, 2), (4, 2), (5, 2)};
            var a1 = new[] {0.1, 0.2, 0.3}.IterPercents(10).ToArray();
            Assert.AreEqual(e1, a1);


            var e2 = new[] {(0, 0), (1, 0)};
            var a2 = new[] {0.2d}.IterPercents(10).ToArray();
            Assert.AreEqual(e2, a2);
        }

        [Test]
        public void TestIterPercentsCoverage()
        {
            for (var i = 0; i < 1000; i++)
            {
                var percents = new[] {i / 1000d, (1000 - i) / 1000d};
                for (var k = 0; k < 1000; k++)
                {
                    var arr = percents.IterPercents(k).ToArray();
                    Assert.AreEqual(k, arr.Length);
                }
            }

            var p = new[] {new[] {0.3,}, new[] {0.1, 0.5}, new[] {0.2, 0.7}, new[] {0.4, 0.05}, new[] {0.5,}};
            foreach (var arr in p)
            {
                for (var i = 0; i < 100; i++)
                {
                    var covered = (int) Math.Ceiling(i * arr.Sum());
                    var actual = arr.IterPercents(i).ToArray();
                    Assert.AreEqual(covered, actual.Length);
                }
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