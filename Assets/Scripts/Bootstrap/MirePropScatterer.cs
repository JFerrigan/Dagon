using Dagon.Core;
using Dagon.Bootstrap.Spawning;
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
            public SpawnedProp(GameObject root)
            {
                Root = root;
            }

            public GameObject Root { get; }
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

        public void Configure(Camera cameraReference, Transform scatterCenterReference = null)
        {
            worldCamera = cameraReference;
            scatterCenter = scatterCenterReference;
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
            renderer.color = new Color(1f, 1f, 1f, 0.92f);

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);

            activeProps[cell] = new SpawnedProp(prop);
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
    }
}
