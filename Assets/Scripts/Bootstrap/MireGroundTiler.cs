using Dagon.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Dagon.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class MireGroundTiler : MonoBehaviour
    {
        private sealed class ActiveTile
        {
            public ActiveTile(GameObject root, Vector2Int coordinate)
            {
                Root = root;
                Coordinate = coordinate;
            }

            public GameObject Root { get; }
            public Vector2Int Coordinate { get; }
        }

        private readonly Dictionary<string, Sprite> spriteCache = new();
        private readonly Dictionary<Vector2Int, ActiveTile> activeTiles = new();
        private readonly List<Vector2Int> tileBuffer = new();

        [SerializeField] private string[] baseTileResourcePaths =
        {
            "Sprites/Tiles/mire_base_a",
            "Sprites/Tiles/mire_base_b",
            "Sprites/Tiles/mire_base_c"
        };
        [SerializeField] private string[] mediumAccentTileResourcePaths =
        {
            "Sprites/Tiles/mire_roots_a",
            "Sprites/Tiles/mire_puddle_a"
        };
        [SerializeField] private string[] rareAccentTileResourcePaths =
        {
            "Sprites/Tiles/mire_bones_a"
        };
        [SerializeField] private string[] overlayTileResourcePaths =
        {
            "Sprites/Tiles/mire_overlay_a",
            "Sprites/Tiles/mire_overlay_b"
        };

        [SerializeField] private int visibleRadiusInTiles = 7;
        [SerializeField] private float tileSize = 4f;
        [SerializeField] private float yOffset = -0.02f;
        [SerializeField] private float overlayChance = 0.08f;

        private Transform trackedTarget;
        private Transform tileRoot;
        private BoxCollider groundCollider;
        private Vector2Int currentCenterTile = new(int.MinValue, int.MinValue);
        private RuntimeBiomeProfile currentBiomeProfile;

        private void Update()
        {
            if (trackedTarget == null || tileRoot == null)
            {
                return;
            }

            var nextCenterTile = WorldToTile(trackedTarget.position);
            if (nextCenterTile == currentCenterTile)
            {
                return;
            }

            currentCenterTile = nextCenterTile;
            RefreshTiles();
        }

        public void Configure(Transform target)
        {
            trackedTarget = target;
        }

        public void ApplyBiomeProfile(RuntimeBiomeProfile profile)
        {
            currentBiomeProfile = profile;
        }

        public void Build()
        {
            if (tileRoot != null)
            {
                return;
            }

            var root = new GameObject("BlackMireGround");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, yOffset, 0f);
            tileRoot = root.transform;

            var colliderRoot = new GameObject("GroundCollider");
            colliderRoot.transform.SetParent(root.transform, false);
            groundCollider = colliderRoot.AddComponent<BoxCollider>();
            groundCollider.center = new Vector3(0f, 0f, 0f);
            groundCollider.size = new Vector3(GetActiveDiameter(), 0.5f, GetActiveDiameter());

            currentCenterTile = trackedTarget != null ? WorldToTile(trackedTarget.position) : Vector2Int.zero;
            RefreshTiles();
        }

        public void RefreshBiomeRadius(Vector3 worldCenter, float radius)
        {
            if (tileRoot == null || activeTiles.Count == 0)
            {
                return;
            }

            var radiusSquared = radius * radius;
            tileBuffer.Clear();
            foreach (var entry in activeTiles)
            {
                if (entry.Value == null || entry.Value.Root == null)
                {
                    tileBuffer.Add(entry.Key);
                    continue;
                }

                var offset = entry.Value.Root.transform.position - worldCenter;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radiusSquared)
                {
                    Destroy(entry.Value.Root);
                    tileBuffer.Add(entry.Key);
                }
            }

            for (var i = 0; i < tileBuffer.Count; i++)
            {
                activeTiles.Remove(tileBuffer[i]);
            }

            tileBuffer.Clear();
            for (var z = currentCenterTile.y - visibleRadiusInTiles; z <= currentCenterTile.y + visibleRadiusInTiles; z++)
            {
                for (var x = currentCenterTile.x - visibleRadiusInTiles; x <= currentCenterTile.x + visibleRadiusInTiles; x++)
                {
                    var coordinate = new Vector2Int(x, z);
                    var offset = new Vector3(
                        (coordinate.x - currentCenterTile.x) * tileSize,
                        0f,
                        (coordinate.y - currentCenterTile.y) * tileSize);
                    var worldPosition = tileRoot.position + offset;
                    var toCenter = worldPosition - worldCenter;
                    toCenter.y = 0f;
                    if (toCenter.sqrMagnitude > radiusSquared || activeTiles.ContainsKey(coordinate))
                    {
                        continue;
                    }

                    CreateTile(coordinate);
                }
            }
        }

        private void RefreshTiles()
        {
            if (tileRoot == null)
            {
                return;
            }

            tileRoot.localPosition = new Vector3(currentCenterTile.x * tileSize, yOffset, currentCenterTile.y * tileSize);

            tileBuffer.Clear();
            foreach (var tileCoordinate in activeTiles.Keys)
            {
                tileBuffer.Add(tileCoordinate);
            }

            for (var z = currentCenterTile.y - visibleRadiusInTiles; z <= currentCenterTile.y + visibleRadiusInTiles; z++)
            {
                for (var x = currentCenterTile.x - visibleRadiusInTiles; x <= currentCenterTile.x + visibleRadiusInTiles; x++)
                {
                    var coordinate = new Vector2Int(x, z);
                    if (activeTiles.ContainsKey(coordinate))
                    {
                        RepositionTile(activeTiles[coordinate].Root.transform, coordinate);
                        tileBuffer.Remove(coordinate);
                        continue;
                    }

                    CreateTile(coordinate);
                }
            }

            for (var i = 0; i < tileBuffer.Count; i++)
            {
                var coordinate = tileBuffer[i];
                if (activeTiles.TryGetValue(coordinate, out var tile))
                {
                    if (tile.Root != null)
                    {
                        Destroy(tile.Root);
                    }

                    activeTiles.Remove(coordinate);
                }
            }

            if (groundCollider != null)
            {
                groundCollider.center = new Vector3(currentCenterTile.x * tileSize, 0f, currentCenterTile.y * tileSize);
                groundCollider.size = new Vector3(GetActiveDiameter(), 0.5f, GetActiveDiameter());
            }
        }

        private void CreateTile(Vector2Int coordinate)
        {
            var sprite = LoadTileSprite(ChooseTilePath(coordinate.x, coordinate.y));
            if (sprite == null)
            {
                return;
            }

            var tile = new GameObject($"Tile_{coordinate.x}_{coordinate.y}");
            tile.transform.SetParent(tileRoot, false);
            tile.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            tile.transform.localScale = new Vector3(tileSize / 4f, tileSize / 4f, 1f);
            tile.transform.localPosition = new Vector3(
                (coordinate.x - currentCenterTile.x) * tileSize,
                0f,
                (coordinate.y - currentCenterTile.y) * tileSize);

            var renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -100;
            renderer.color = currentBiomeProfile != null ? currentBiomeProfile.GroundTint : Color.white;

            CreateOverlayTile(tile.transform, coordinate);

            activeTiles[coordinate] = new ActiveTile(tile, coordinate);
        }

        private void RepositionTile(Transform tileTransform, Vector2Int coordinate)
        {
            if (tileTransform == null)
            {
                return;
            }

            tileTransform.localPosition = new Vector3(
                (coordinate.x - currentCenterTile.x) * tileSize,
                0f,
                (coordinate.y - currentCenterTile.y) * tileSize);
        }

        private void CreateOverlayTile(Transform parent, Vector2Int coordinate)
        {
            var overlayPath = ChooseOverlayPath(coordinate.x, coordinate.y);
            if (string.IsNullOrWhiteSpace(overlayPath))
            {
                return;
            }

            var overlaySprite = LoadTileSprite(overlayPath);
            if (overlaySprite == null)
            {
                return;
            }

            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(parent, false);
            overlay.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale = Vector3.one;

            var overlayRenderer = overlay.AddComponent<SpriteRenderer>();
            overlayRenderer.sprite = overlaySprite;
            overlayRenderer.sortingOrder = -90;
            overlayRenderer.color = currentBiomeProfile != null ? currentBiomeProfile.OverlayTint : new Color(1f, 1f, 1f, 0.92f);
        }

        private string ChooseTilePath(int x, int z)
        {
            var activeBaseTiles = ResolvePaths(currentBiomeProfile != null ? currentBiomeProfile.BaseTileResourcePaths : null, baseTileResourcePaths);
            var activeMediumAccents = ResolvePaths(currentBiomeProfile != null ? currentBiomeProfile.MediumAccentTileResourcePaths : null, mediumAccentTileResourcePaths);
            var activeRareAccents = ResolvePaths(currentBiomeProfile != null ? currentBiomeProfile.RareAccentTileResourcePaths : null, rareAccentTileResourcePaths);
            var accentNoise = SampleNoise(x, z, 0.073f, 0.041f, 13.7f, 7.3f);
            var detailNoise = SampleNoise(x, z, 0.19f, 0.16f, 4.1f, 19.4f);

            if (activeRareAccents != null && activeRareAccents.Length > 0 && accentNoise > 0.83f)
            {
                return activeRareAccents[ChooseIndex(activeRareAccents.Length, detailNoise)];
            }

            if (activeMediumAccents != null && activeMediumAccents.Length > 0 && accentNoise > 0.58f)
            {
                return activeMediumAccents[ChooseIndex(activeMediumAccents.Length, detailNoise)];
            }

            if (activeBaseTiles == null || activeBaseTiles.Length == 0)
            {
                return null;
            }

            var regionNoise = SampleNoise(x, z, 0.052f, 0.052f, 0f, 0f);
            var variantNoise = SampleNoise(x, z, 0.11f, 0.11f, 33.2f, 71.9f);
            var groupedChoice = Mathf.Clamp01((regionNoise * 0.72f) + (variantNoise * 0.28f));
            return activeBaseTiles[ChooseIndex(activeBaseTiles.Length, groupedChoice)];
        }

        private string ChooseOverlayPath(int x, int z)
        {
            var activeOverlays = ResolvePaths(currentBiomeProfile != null ? currentBiomeProfile.OverlayTileResourcePaths : null, overlayTileResourcePaths);
            if (activeOverlays == null || activeOverlays.Length == 0)
            {
                return null;
            }

            var clusterNoise = SampleNoise(x, z, 0.045f, 0.045f, 91.7f, 44.3f);
            var breakupNoise = SampleNoise(x, z, 0.17f, 0.17f, 8.4f, 57.6f);
            var overlaySignal = (clusterNoise * 0.78f) + (breakupNoise * 0.22f);
            if (overlaySignal < 1f - overlayChance)
            {
                return null;
            }

            var selectionNoise = SampleNoise(x, z, 0.12f, 0.12f, 140.2f, 22.6f);
            return activeOverlays[ChooseIndex(activeOverlays.Length, selectionNoise)];
        }

        private Sprite LoadTileSprite(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            if (spriteCache.TryGetValue(resourcePath, out var cachedSprite))
            {
                return cachedSprite;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"Missing ground tile texture at Resources/{resourcePath}");
                return null;
            }

            texture.filterMode = FilterMode.Point;
            var pixelsPerUnit = texture.width / tileSize;
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            spriteCache[resourcePath] = sprite;
            return sprite;
        }

        private Vector2Int WorldToTile(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt((worldPosition.x / tileSize) + 0.5f),
                Mathf.FloorToInt((worldPosition.z / tileSize) + 0.5f));
        }

        private float GetActiveDiameter()
        {
            return ((visibleRadiusInTiles * 2) + 1) * tileSize;
        }

        private static int ChooseIndex(int length, float sample)
        {
            if (length <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(sample) * length), 0, length - 1);
        }

        private static float SampleNoise(int x, int z, float scaleX, float scaleZ, float offsetX, float offsetZ)
        {
            return Mathf.PerlinNoise((x * scaleX) + offsetX, (z * scaleZ) + offsetZ);
        }

        private static string[] ResolvePaths(string[] primaryPaths, string[] fallbackPaths)
        {
            return primaryPaths != null && primaryPaths.Length > 0 ? primaryPaths : fallbackPaths;
        }
    }
}
