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
                    var cell = new Vector2Int(x, z);
                    if (!IsCellVisible(centerCell, cell))
                    {
                        continue;
                    }

                    visibleCells.Add(cell);
                }
            }

            return visibleCells;
        }

        public bool TryPlanProp(Vector2Int cell, int visualCount, out PropPlacement placement)
        {
            placement = default;
            if (visualCount <= 0 || Sample01(cell.x, cell.y, 0) > GetLocalSpawnChance(cell))
            {
                return false;
            }

            var visualIndex = Mathf.Clamp(Mathf.FloorToInt(Sample01(cell.x, cell.y, 1) * visualCount), 0, visualCount - 1);
            var drift = GetClusterDrift(cell);
            var offsetX = Mathf.Lerp(-cellSize * 0.56f, cellSize * 0.56f, Sample01(cell.x, cell.y, 3)) + drift.x;
            var offsetZ = Mathf.Lerp(-cellSize * 0.56f, cellSize * 0.56f, Sample01(cell.x, cell.y, 4)) + drift.y;
            offsetX = Mathf.Clamp(offsetX, -cellSize * 0.82f, cellSize * 0.82f);
            offsetZ = Mathf.Clamp(offsetZ, -cellSize * 0.82f, cellSize * 0.82f);
            var position = new Vector3((cell.x * cellSize) + offsetX, 0.03f, (cell.y * cellSize) + offsetZ);
            var scaleMultiplier = Mathf.Lerp(0.8f, 1.15f, Sample01(cell.x, cell.y, 2));

            placement = new PropPlacement(cell, visualIndex, position, scaleMultiplier);
            return true;
        }

        private float GetLocalSpawnChance(Vector2Int cell)
        {
            var coarseX = Mathf.FloorToInt(cell.x * 0.5f);
            var coarseZ = Mathf.FloorToInt(cell.y * 0.5f);
            var clusterBias = Mathf.Lerp(-0.18f, 0.14f, Sample01(coarseX, coarseZ, 6));
            return Mathf.Clamp01(propSpawnChance + clusterBias);
        }

        private Vector2 GetClusterDrift(Vector2Int cell)
        {
            var coarseX = Mathf.FloorToInt(cell.x * 0.5f);
            var coarseZ = Mathf.FloorToInt(cell.y * 0.5f);
            var driftX = Mathf.Lerp(-cellSize * 0.28f, cellSize * 0.28f, Sample01(coarseX, coarseZ, 7));
            var driftZ = Mathf.Lerp(-cellSize * 0.28f, cellSize * 0.28f, Sample01(coarseX, coarseZ, 8));
            return new Vector2(driftX, driftZ);
        }

        private bool IsCellVisible(Vector2Int centerCell, Vector2Int cell)
        {
            var offset = cell - centerCell;
            var distance = new Vector2(offset.x, offset.y).magnitude;
            if (distance <= visibleRadiusInCells - 0.35f)
            {
                return true;
            }

            var edgeNoise = Mathf.Lerp(-0.3f, 0.3f, Sample01(cell.x, cell.y, 5));
            return distance <= visibleRadiusInCells + edgeNoise;
        }

        public static float Sample01(int x, int z, int salt)
        {
            var hash = (x * 73856093) ^ (z * 19349663) ^ (salt * 83492791);
            hash &= 0x7fffffff;
            return hash / (float)int.MaxValue;
        }
    }
}
