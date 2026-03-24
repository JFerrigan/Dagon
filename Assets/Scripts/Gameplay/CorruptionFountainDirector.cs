using Dagon.Core;
using UnityEngine;
using System.Collections.Generic;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CorruptionFountainDirector : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RunStateManager runStateManager;
        [SerializeField] private CorruptionMeter corruptionMeter;
        [SerializeField] private float cleanseAmount = 25f;
        [SerializeField] private int visibleRadiusInLandmarkCells = 2;
        [SerializeField] private float landmarkCellSize = 180f;
        [SerializeField] private float landmarkSpawnChance = 0.95f;
        [SerializeField] private float spawnMargin = 18f;

        private readonly Dictionary<Vector2Int, CorruptionFountain> activeFountains = new();
        private readonly HashSet<Vector2Int> depletedFountainCells = new();
        private readonly List<Vector2Int> cellBuffer = new();
        private Vector2Int currentCenterCell = new(int.MinValue, int.MinValue);
        private FountainScatterPlanner planner;

        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (corruptionMeter == null || player == null || worldCamera == null)
            {
                return;
            }

            planner ??= new FountainScatterPlanner(visibleRadiusInLandmarkCells, landmarkCellSize, landmarkSpawnChance, spawnMargin);
            RefreshFountains(force: false);
            CleanupDestroyedEntries();
        }

        public void Configure(Transform playerTransform, Camera cameraReference, RunStateManager runState, CorruptionMeter meter)
        {
            player = playerTransform;
            worldCamera = cameraReference;
            runStateManager = runState;
            corruptionMeter = meter;
        }

        private void Start()
        {
            planner = new FountainScatterPlanner(visibleRadiusInLandmarkCells, landmarkCellSize, landmarkSpawnChance, spawnMargin);
            RefreshFountains(force: true);
        }

        private void RefreshFountains(bool force)
        {
            planner ??= new FountainScatterPlanner(visibleRadiusInLandmarkCells, landmarkCellSize, landmarkSpawnChance, spawnMargin);
            var centerCell = planner.WorldToCell(player.position);
            if (!force && centerCell == currentCenterCell)
            {
                return;
            }

            currentCenterCell = centerCell;
            cellBuffer.Clear();
            foreach (var key in activeFountains.Keys)
            {
                cellBuffer.Add(key);
            }

            var visibleCells = planner.GetVisibleCells(centerCell);
            for (var i = 0; i < visibleCells.Count; i++)
            {
                var cell = visibleCells[i];
                if (activeFountains.ContainsKey(cell))
                {
                    cellBuffer.Remove(cell);
                    continue;
                }

                if (!planner.TryPlanFountain(cell, out var position))
                {
                    continue;
                }

                var fountain = CorruptionFountain.Create(
                    position,
                    cleanseAmount,
                    worldCamera,
                    corruptionMeter,
                    cell,
                    depletedFountainCells.Contains(cell));
                fountain.transform.SetParent(transform, true);
                activeFountains[cell] = fountain;
            }

            for (var i = 0; i < cellBuffer.Count; i++)
            {
                var cell = cellBuffer[i];
                if (!activeFountains.TryGetValue(cell, out var fountain))
                {
                    continue;
                }

                if (fountain != null)
                {
                    if (fountain.IsDepleted)
                    {
                        depletedFountainCells.Add(cell);
                    }

                    Destroy(fountain.gameObject);
                }

                activeFountains.Remove(cell);
            }
        }

        private void CleanupDestroyedEntries()
        {
            cellBuffer.Clear();
            foreach (var entry in activeFountains)
            {
                if (entry.Value != null)
                {
                    if (entry.Value.IsDepleted)
                    {
                        depletedFountainCells.Add(entry.Key);
                    }

                    continue;
                }

                cellBuffer.Add(entry.Key);
            }

            for (var i = 0; i < cellBuffer.Count; i++)
            {
                activeFountains.Remove(cellBuffer[i]);
            }
        }

        private sealed class FountainScatterPlanner
        {
            private readonly int visibleRadiusInCells;
            private readonly float cellSize;
            private readonly float spawnChance;
            private readonly float margin;

            public FountainScatterPlanner(int visibleRadiusInCells, float cellSize, float spawnChance, float margin)
            {
                this.visibleRadiusInCells = Mathf.Max(0, visibleRadiusInCells);
                this.cellSize = Mathf.Max(1f, cellSize);
                this.spawnChance = Mathf.Clamp01(spawnChance);
                this.margin = Mathf.Clamp(margin, 0f, cellSize * 0.4f);
            }

            public Vector2Int WorldToCell(Vector3 worldPosition)
            {
                return new Vector2Int(
                    Mathf.RoundToInt(worldPosition.x / cellSize),
                    Mathf.RoundToInt(worldPosition.z / cellSize));
            }

            public List<Vector2Int> GetVisibleCells(Vector2Int centerCell)
            {
                var cells = new List<Vector2Int>(((visibleRadiusInCells * 2) + 1) * ((visibleRadiusInCells * 2) + 1));
                for (var z = centerCell.y - visibleRadiusInCells; z <= centerCell.y + visibleRadiusInCells; z++)
                {
                    for (var x = centerCell.x - visibleRadiusInCells; x <= centerCell.x + visibleRadiusInCells; x++)
                    {
                        cells.Add(new Vector2Int(x, z));
                    }
                }

                return cells;
            }

            public bool TryPlanFountain(Vector2Int cell, out Vector3 position)
            {
                position = default;
                if (Sample01(cell.x, cell.y, 0) > spawnChance)
                {
                    return false;
                }

                var offsetLimit = Mathf.Max(1f, (cellSize * 0.5f) - margin);
                var offsetX = Mathf.Lerp(-offsetLimit, offsetLimit, Sample01(cell.x, cell.y, 1));
                var offsetZ = Mathf.Lerp(-offsetLimit, offsetLimit, Sample01(cell.x, cell.y, 2));
                position = new Vector3((cell.x * cellSize) + offsetX, 0f, (cell.y * cellSize) + offsetZ);
                return true;
            }

            private static float Sample01(int x, int z, int salt)
            {
                var hash = (x * 73856093) ^ (z * 19349663) ^ (salt * 83492791);
                hash &= 0x7fffffff;
                return hash / (float)int.MaxValue;
            }
        }
    }
}
