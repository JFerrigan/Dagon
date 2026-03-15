using Dagon.Bootstrap.Spawning;
using NUnit.Framework;
using UnityEngine;

namespace Dagon.Tests.Editor
{
    public sealed class PropScatterPlannerTests
    {
        [Test]
        public void WorldToCell_RoundsUsingConfiguredCellSize()
        {
            var planner = new PropScatterPlanner(4, 8f, 0.6f);
            var cell = planner.WorldToCell(new Vector3(15.9f, 0f, -8.2f));

            Assert.AreEqual(new Vector2Int(2, -1), cell);
        }

        [Test]
        public void GetVisibleCells_ReturnsSquareAroundCenter()
        {
            var planner = new PropScatterPlanner(1, 8f, 1f);
            var cells = planner.GetVisibleCells(new Vector2Int(3, 5));

            Assert.AreEqual(9, cells.Count);
            Assert.Contains(new Vector2Int(2, 4), cells);
            Assert.Contains(new Vector2Int(3, 5), cells);
            Assert.Contains(new Vector2Int(4, 6), cells);
        }

        [Test]
        public void TryPlanProp_IsDeterministicForSameCell()
        {
            var planner = new PropScatterPlanner(4, 8f, 1f);

            var firstSucceeded = planner.TryPlanProp(new Vector2Int(2, 3), 5, out var first);
            var secondSucceeded = planner.TryPlanProp(new Vector2Int(2, 3), 5, out var second);

            Assert.IsTrue(firstSucceeded);
            Assert.IsTrue(secondSucceeded);
            Assert.AreEqual(first.VisualIndex, second.VisualIndex);
            Assert.AreEqual(first.Position, second.Position);
            Assert.AreEqual(first.ScaleMultiplier, second.ScaleMultiplier);
        }

        [Test]
        public void TryPlanProp_RespectsSpawnChance()
        {
            var planner = new PropScatterPlanner(4, 8f, 0f);
            var succeeded = planner.TryPlanProp(new Vector2Int(2, 3), 5, out _);

            Assert.IsFalse(succeeded);
        }
    }
}
