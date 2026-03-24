using Dagon.Core;
using Dagon.Bootstrap.Spawning;
using Dagon.Gameplay;
using Dagon.Rendering;
using System.Collections.Generic;
using UnityEngine;

namespace Dagon.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class MirePropScatterer : MonoBehaviour
    {
        private sealed class SpawnedProp
        {
            public SpawnedProp(GameObject root, Vector2Int coordinate, SpriteRenderer renderer)
            {
                Root = root;
                Coordinate = coordinate;
                Renderer = renderer;
            }

            public GameObject Root { get; }
            public Vector2Int Coordinate { get; }
            public SpriteRenderer Renderer { get; }
        }

        private readonly struct PropDefinition
        {
            public PropDefinition(string name, string resourcePath, Vector3 baseScale, int sortingOrder)
            {
                Name = name;
                ResourcePath = resourcePath;
                BaseScale = baseScale;
                SortingOrder = sortingOrder;
            }

            public string Name { get; }
            public string ResourcePath { get; }
            public Vector3 BaseScale { get; }
            public int SortingOrder { get; }
        }

        private readonly struct PropVisual
        {
            public PropVisual(string name, Sprite sprite, Vector3 baseScale, int sortingOrder)
            {
                Name = name;
                Sprite = sprite;
                BaseScale = baseScale;
                SortingOrder = sortingOrder;
            }

            public string Name { get; }
            public Sprite Sprite { get; }
            public Vector3 BaseScale { get; }
            public int SortingOrder { get; }
        }

        private static readonly PropDefinition[] PropDefinitions =
        {
            new PropDefinition("HarpoonProp", "Sprites/Props/harpoon_ground_prop", new Vector3(0.05f, 0.05f, 1f), 1),
            new PropDefinition("BarrelProp", "Sprites/Props/barrel_ground_prop", new Vector3(0.11f, 0.11f, 1f), 2),
            new PropDefinition("FishPileProp", "Sprites/Props/fish_pile_prop", new Vector3(0.11f, 0.11f, 1f), 1),
            new PropDefinition("LeviathanCarcassProp", "Sprites/Props/leviathan_carcass_prop", new Vector3(0.16f, 0.16f, 1f), 0),
            new PropDefinition("Carcass2Prop", "Sprites/Props/carcass2_prop", new Vector3(0.15f, 0.15f, 1f), 0),
            new PropDefinition("SkullMoundProp", "Sprites/Props/skull_mound_prop", new Vector3(0.15f, 0.15f, 1f), 0)
        };

        [SerializeField] private Camera worldCamera;
        [SerializeField] private int visibleRadiusInCells = 4;
        [SerializeField] private float cellSize = 8f;
        [SerializeField] private float propSpawnChance = 0.6f;
        [SerializeField] private Transform scatterCenter;

        private PropVisual[] loadedPropVisuals;
        private readonly Dictionary<Vector2Int, SpawnedProp> activeProps = new();
        private readonly List<Vector2Int> cellBuffer = new();
        private Vector2Int currentCenterCell = new(int.MinValue, int.MinValue);
        private PropScatterPlanner planner;
        private RuntimeBiomeProfile currentBiomeProfile;
        private WorldProgressionDirector progressionDirector;

        public void Configure(Camera cameraReference, Transform scatterCenterReference = null)
        {
            worldCamera = cameraReference;
            scatterCenter = scatterCenterReference;
        }

        public void ApplyBiomeProfile(RuntimeBiomeProfile profile)
        {
            currentBiomeProfile = profile;
        }

        public void ConfigureProgression(WorldProgressionDirector director)
        {
            progressionDirector = director;
            RefreshProgressionPresentation();
        }

        private void Start()
        {
            loadedPropVisuals = LoadPropVisuals();

            if (worldCamera == null)
            {
                return;
            }

            if (loadedPropVisuals.Length == 0)
            {
                return;
            }

            planner = new PropScatterPlanner(visibleRadiusInCells, cellSize, propSpawnChance);
            RefreshProps(force: true);
        }

        private void Update()
        {
            if (loadedPropVisuals == null || loadedPropVisuals.Length == 0)
            {
                return;
            }

            planner ??= new PropScatterPlanner(visibleRadiusInCells, cellSize, propSpawnChance);
            RefreshProps(force: false);
            RefreshProgressionPresentation();
        }

        private void RefreshProps(bool force)
        {
            planner ??= new PropScatterPlanner(visibleRadiusInCells, cellSize, propSpawnChance);
            var centerCell = planner.WorldToCell(scatterCenter != null ? scatterCenter.position : transform.position);
            if (!force && centerCell == currentCenterCell)
            {
                return;
            }

            currentCenterCell = centerCell;
            cellBuffer.Clear();
            foreach (var key in activeProps.Keys)
            {
                cellBuffer.Add(key);
            }

            var visibleCells = planner.GetVisibleCells(centerCell);
            for (var i = 0; i < visibleCells.Count; i++)
            {
                var cell = visibleCells[i];
                if (activeProps.ContainsKey(cell))
                {
                    cellBuffer.Remove(cell);
                    continue;
                }

                TryCreateProp(cell);
            }

            for (var i = 0; i < cellBuffer.Count; i++)
            {
                var cell = cellBuffer[i];
                if (!activeProps.TryGetValue(cell, out var prop))
                {
                    continue;
                }

                if (prop.Root != null)
                {
                    Destroy(prop.Root);
                }

                activeProps.Remove(cell);
            }

            RefreshProgressionPresentation();
        }

        public void RefreshBiomeRadius(Vector3 worldCenter, float radius)
        {
            if (activeProps.Count == 0)
            {
                return;
            }

            var radiusSquared = radius * radius;
            cellBuffer.Clear();
            foreach (var entry in activeProps)
            {
                if (entry.Value == null || entry.Value.Root == null)
                {
                    cellBuffer.Add(entry.Key);
                    continue;
                }

                var offset = entry.Value.Root.transform.position - worldCenter;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radiusSquared)
                {
                    Destroy(entry.Value.Root);
                    cellBuffer.Add(entry.Key);
                }
            }

            for (var i = 0; i < cellBuffer.Count; i++)
            {
                activeProps.Remove(cellBuffer[i]);
            }

            cellBuffer.Clear();
            planner ??= new PropScatterPlanner(visibleRadiusInCells, cellSize, propSpawnChance);
            var visibleCells = planner.GetVisibleCells(currentCenterCell);
            for (var i = 0; i < visibleCells.Count; i++)
            {
                var cell = visibleCells[i];
                var worldPosition = new Vector3(cell.x * cellSize, 0.03f, cell.y * cellSize);
                var toCenter = worldPosition - worldCenter;
                toCenter.y = 0f;
                if (toCenter.sqrMagnitude > radiusSquared || activeProps.ContainsKey(cell))
                {
                    continue;
                }

                TryCreateProp(cell);
            }

            RefreshProgressionPresentation();
        }

        public void RefreshProgressionPresentation()
        {
            foreach (var entry in activeProps)
            {
                var prop = entry.Value;
                if (prop == null || prop.Root == null || prop.Renderer == null)
                {
                    continue;
                }

                ApplyPropPresentation(prop);
            }
        }

        private void TryCreateProp(Vector2Int cell)
        {
            planner ??= new PropScatterPlanner(visibleRadiusInCells, cellSize, propSpawnChance);
            if (!planner.TryPlanProp(cell, loadedPropVisuals.Length, out var placement))
            {
                return;
            }

            var visual = loadedPropVisuals[placement.VisualIndex];

            var prop = new GameObject($"{visual.Name}_{cell.x}_{cell.y}");
            prop.transform.SetParent(transform, true);
            prop.transform.position = placement.Position;

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(prop.transform, false);
            visuals.transform.localPosition = Vector3.zero;
            visuals.transform.localScale = visual.BaseScale * placement.ScaleMultiplier;

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = visual.Sprite;
            renderer.sortingOrder = visual.SortingOrder;
            renderer.color = Color.white;

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);

            var spawnedProp = new SpawnedProp(prop, cell, renderer);
            activeProps[cell] = spawnedProp;
            ApplyPropPresentation(spawnedProp);
        }

        private static PropVisual[] LoadPropVisuals()
        {
            var loadedCount = 0;
            var visuals = new PropVisual[PropDefinitions.Length];
            for (var i = 0; i < PropDefinitions.Length; i++)
            {
                var definition = PropDefinitions[i];
                var sprite = RuntimeSpriteLibrary.LoadSprite(definition.ResourcePath);
                if (sprite == null)
                {
                    continue;
                }

                visuals[loadedCount++] = new PropVisual(definition.Name, sprite, definition.BaseScale, definition.SortingOrder);
            }

            if (loadedCount == visuals.Length)
            {
                return visuals;
            }

            var trimmedVisuals = new PropVisual[loadedCount];
            for (var i = 0; i < loadedCount; i++)
            {
                trimmedVisuals[i] = visuals[i];
            }

            return trimmedVisuals;
        }

        private RuntimeBiomeProfile ResolveBiomeProfile(Vector2Int cell)
        {
            if (progressionDirector == null)
            {
                return currentBiomeProfile;
            }

            var worldPosition = new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
            return progressionDirector.ResolveBiomeAtPosition(worldPosition) ?? currentBiomeProfile;
        }

        private void ApplyPropPresentation(SpawnedProp prop)
        {
            var profile = ResolveBiomeProfile(prop.Coordinate);
            var tint = profile != null ? profile.PropTint : new Color(1f, 1f, 1f, 0.92f);
            var worldPosition = new Vector3(prop.Coordinate.x * cellSize, 0f, prop.Coordinate.y * cellSize);
            var corrupted = progressionDirector != null && progressionDirector.IsPositionCorrupted(worldPosition);
            prop.Renderer.color = corrupted ? ApplyCorruptionShadow(tint) : tint;
        }

        private static Color ApplyCorruptionShadow(Color source)
        {
            return new Color(source.r * 0.42f, source.g * 0.44f, source.b * 0.50f, source.a);
        }
    }
}
