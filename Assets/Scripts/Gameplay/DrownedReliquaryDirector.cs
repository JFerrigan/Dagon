using System.Collections.Generic;
using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DrownedReliquaryDirector : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private PlayerCombatLoadout combatLoadout;
        [SerializeField] private Health playerHealth;
        [SerializeField] private CorruptionMeter corruptionMeter;
        [SerializeField] private int visibleRadiusInLandmarkCells = 2;
        [SerializeField] private float landmarkCellSize = 180f;
        [SerializeField] private float landmarkSpawnChance = 0.52f;
        [SerializeField] private float spawnMargin = 22f;

        private readonly Dictionary<Vector2Int, DrownedReliquary> activeAltars = new();
        private readonly HashSet<Vector2Int> depletedAltarCells = new();
        private readonly List<Vector2Int> cellBuffer = new();
        private Vector2Int currentCenterCell = new(int.MinValue, int.MinValue);
        private ReliquaryScatterPlanner planner;

        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (player == null || worldCamera == null || combatLoadout == null || playerHealth == null || corruptionMeter == null)
            {
                return;
            }

            planner ??= new ReliquaryScatterPlanner(visibleRadiusInLandmarkCells, landmarkCellSize, landmarkSpawnChance, spawnMargin);
            RefreshAltars(force: false);
            CleanupDestroyedEntries();
        }

        public void Configure(Transform playerTransform, Camera cameraReference, PlayerCombatLoadout loadout, Health health, CorruptionMeter meter)
        {
            player = playerTransform;
            worldCamera = cameraReference;
            combatLoadout = loadout;
            playerHealth = health;
            corruptionMeter = meter;
        }

        private void Start()
        {
            planner = new ReliquaryScatterPlanner(visibleRadiusInLandmarkCells, landmarkCellSize, landmarkSpawnChance, spawnMargin);
            RefreshAltars(force: true);
        }

        private void RefreshAltars(bool force)
        {
            planner ??= new ReliquaryScatterPlanner(visibleRadiusInLandmarkCells, landmarkCellSize, landmarkSpawnChance, spawnMargin);
            var centerCell = planner.WorldToCell(player.position);
            if (!force && centerCell == currentCenterCell)
            {
                return;
            }

            currentCenterCell = centerCell;
            cellBuffer.Clear();
            foreach (var key in activeAltars.Keys)
            {
                cellBuffer.Add(key);
            }

            var visibleCells = planner.GetVisibleCells(centerCell);
            for (var i = 0; i < visibleCells.Count; i++)
            {
                var cell = visibleCells[i];
                if (activeAltars.ContainsKey(cell))
                {
                    cellBuffer.Remove(cell);
                    continue;
                }

                if (!planner.TryPlanReliquary(cell, out var position))
                {
                    continue;
                }

                var altar = DrownedReliquary.Create(
                    position,
                    worldCamera,
                    combatLoadout,
                    playerHealth,
                    corruptionMeter,
                    cell,
                    depletedAltarCells.Contains(cell),
                    1f);
                altar.transform.SetParent(transform, true);
                activeAltars[cell] = altar;
            }

            for (var i = 0; i < cellBuffer.Count; i++)
            {
                var cell = cellBuffer[i];
                if (!activeAltars.TryGetValue(cell, out var altar))
                {
                    continue;
                }

                if (altar != null)
                {
                    if (altar.IsDepleted)
                    {
                        depletedAltarCells.Add(cell);
                    }

                    Destroy(altar.gameObject);
                }

                activeAltars.Remove(cell);
            }
        }

        private void CleanupDestroyedEntries()
        {
            cellBuffer.Clear();
            foreach (var entry in activeAltars)
            {
                if (entry.Value != null)
                {
                    if (entry.Value.IsDepleted)
                    {
                        depletedAltarCells.Add(entry.Key);
                    }

                    continue;
                }

                cellBuffer.Add(entry.Key);
            }

            for (var i = 0; i < cellBuffer.Count; i++)
            {
                activeAltars.Remove(cellBuffer[i]);
            }
        }

        private sealed class ReliquaryScatterPlanner
        {
            private readonly int visibleRadiusInCells;
            private readonly float cellSize;
            private readonly float spawnChance;
            private readonly float margin;

            public ReliquaryScatterPlanner(int visibleRadiusInCells, float cellSize, float spawnChance, float margin)
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

            public bool TryPlanReliquary(Vector2Int cell, out Vector3 position)
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
