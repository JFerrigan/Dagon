using System.Collections.Generic;
using Dagon.Gameplay.Spawning;
using NUnit.Framework;
using UnityEngine;

namespace Dagon.Tests.Editor
{
    public sealed class EnemySpawnPlannerTests
    {
        [Test]
        public void SurroundRing_BuildsRequestedCountWithinExpectedRadius()
        {
            var planner = new EnemySpawnPlanner(10f);
            var positions = planner.BuildPatternPositions(
                EnemySpawnPattern.SurroundRing,
                4,
                Vector3.zero,
                Vector3.forward,
                new FixedSpawnRandom(0f, 10f, 0f, 10f, 0f, 10f, 0f, 10f));

            Assert.AreEqual(4, positions.Count);
            for (var index = 0; index < positions.Count; index++)
            {
                var flat = positions[index];
                flat.y = 0f;
                Assert.That(flat.magnitude, Is.InRange(7.8f, 10f));
                Assert.AreEqual(0.5f, positions[index].y, 0.001f);
            }
        }

        [Test]
        public void FrontCone_FallsBackToForwardWhenInputDirectionIsZero()
        {
            var planner = new EnemySpawnPlanner(10f);
            var positions = planner.BuildPatternPositions(
                EnemySpawnPattern.FrontCone,
                1,
                Vector3.zero,
                Vector3.zero,
                new FixedSpawnRandom(0f, 9f));

            Assert.AreEqual(1, positions.Count);
            Assert.Greater(positions[0].z, 0f);
            Assert.AreEqual(0f, positions[0].x, 0.001f);
        }

        [Test]
        public void EliteEscort_AlwaysBuildsFourSlots()
        {
            var planner = new EnemySpawnPlanner(10f);
            var positions = planner.BuildPatternPositions(
                EnemySpawnPattern.EliteEscort,
                99,
                new Vector3(2f, 0f, 3f),
                Vector3.right,
                new FixedSpawnRandom());

            Assert.AreEqual(4, positions.Count);
        }

        private sealed class FixedSpawnRandom : ISpawnRandom
        {
            private readonly Queue<float> values;

            public FixedSpawnRandom(params float[] values)
            {
                this.values = new Queue<float>(values);
            }

            public float Range(float minInclusive, float maxInclusive)
            {
                if (values.Count == 0)
                {
                    return minInclusive;
                }

                return Mathf.Clamp(values.Dequeue(), minInclusive, maxInclusive);
            }
        }
    }
}
