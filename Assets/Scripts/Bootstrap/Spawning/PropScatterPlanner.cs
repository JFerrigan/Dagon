using System.Collections.Generic;
using UnityEngine;

namespace Dagon.Bootstrap.Spawning
{
    public readonly struct PropPlacement
    {
        public PropPlacement(Vector2Int cell, int visualIndex, Vector3 position, float scaleMultiplier)
        {
            Cell = cell;
            VisualIndex = visualIndex;
            Position = position;
            ScaleMultiplier = scaleMultiplier;
        }

        public Vector2Int Cell { get; }
        public int VisualIndex { get; }
        public Vector3 Position { get; }
        public float ScaleMultiplier { get; }
    }

    public sealed class PropScatterPlanner
    {
        private readonly int visibleRadiusInCells;
        private readonly float cellSize;
        private readonly float propSpawnChance;

        public PropScatterPlanner(int visibleRadiusInCells, float cellSize, float propSpawnChance)
        {
            this.visibleRadiusInCells = Mathf.Max(0, visibleRadiusInCells);
            this.cellSize = Mathf.Max(0.1f, cellSize);
            this.propSpawnChance = Mathf.Clamp01(propSpawnChance);
        }

        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / cellSize),
                Mathf.RoundToInt(worldPosition.z / cellSize));
        }

        public List<Vector2Int> GetVisibleCells(Vector2Int centerCell)
        {
            var visibleCells = new List<Vector2Int>(((visibleRadiusInCells * 2) + 1) * ((visibleRadiusInCells * 2) + 1));
            for (var z = centerCell.y - visibleRadiusInCells; z <= centerCell.y + visibleRadiusInCells; z++)
            {
                for (var x = centerCell.x - visibleRadiusInCells; x <= centerCell.x + visibleRadiusInCells; x++)
                {
                    visibleCells.Add(new Vector2Int(x, z));
                }
            }

            return visibleCells;
        }

        public bool TryPlanProp(Vector2Int cell, int visualCount, out PropPlacement placement)
        {
            placement = default;
            if (visualCount <= 0 || Sample01(cell.x, cell.y, 0) > propSpawnChance)
            {
                return false;
            }

            var visualIndex = Mathf.Clamp(Mathf.FloorToInt(Sample01(cell.x, cell.y, 1) * visualCount), 0, visualCount - 1);
            var offsetX = Mathf.Lerp(-cellSize * 0.35f, cellSize * 0.35f, Sample01(cell.x, cell.y, 3));
            var offsetZ = Mathf.Lerp(-cellSize * 0.35f, cellSize * 0.35f, Sample01(cell.x, cell.y, 4));
            var position = new Vector3((cell.x * cellSize) + offsetX, 0.03f, (cell.y * cellSize) + offsetZ);
            var scaleMultiplier = Mathf.Lerp(0.8f, 1.15f, Sample01(cell.x, cell.y, 2));

            placement = new PropPlacement(cell, visualIndex, position, scaleMultiplier);
            return true;
        }

        public static float Sample01(int x, int z, int salt)
        {
            var hash = (x * 73856093) ^ (z * 19349663) ^ (salt * 83492791);
            hash &= 0x7fffffff;
            return hash / (float)int.MaxValue;
        }
    }
}
