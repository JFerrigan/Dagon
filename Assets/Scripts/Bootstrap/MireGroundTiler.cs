using Dagon.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Dagon.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class MireGroundTiler : MonoBehaviour
    {
        private readonly Dictionary<string, Sprite> spriteCache = new();

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

        [SerializeField] private int tilesX = 10;
        [SerializeField] private int tilesZ = 10;
        [SerializeField] private float tileSize = 4f;
        [SerializeField] private float yOffset = -0.02f;

        public void Build()
        {
            var root = new GameObject("BlackMireGround");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, yOffset, 0f);

            var colliderRoot = GameObject.CreatePrimitive(PrimitiveType.Plane);
            colliderRoot.name = "GroundCollider";
            colliderRoot.transform.SetParent(root.transform, false);
            colliderRoot.transform.localScale = new Vector3((tilesX * tileSize) / 10f, 1f, (tilesZ * tileSize) / 10f);

            var colliderRenderer = colliderRoot.GetComponent<MeshRenderer>();
            if (colliderRenderer != null)
            {
                colliderRenderer.enabled = false;
            }

            var halfWidth = (tilesX * tileSize) * 0.5f;
            var halfDepth = (tilesZ * tileSize) * 0.5f;

            for (var z = 0; z < tilesZ; z++)
            {
                for (var x = 0; x < tilesX; x++)
                {
                    var sprite = LoadTileSprite(ChooseTilePath(x, z));
                    if (sprite == null)
                    {
                        continue;
                    }

                    var tile = new GameObject($"Tile_{x}_{z}");
                    tile.name = $"Tile_{x}_{z}";
                    tile.transform.SetParent(root.transform, false);
                    tile.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    tile.transform.localScale = new Vector3(tileSize / 4f, tileSize / 4f, 1f);
                    tile.transform.localPosition = new Vector3(
                        (x * tileSize) - halfWidth + (tileSize * 0.5f),
                        0f,
                        (z * tileSize) - halfDepth + (tileSize * 0.5f));

                    var renderer = tile.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sortingOrder = -100;
                }
            }
        }

        private string ChooseTilePath(int x, int z)
        {
            var seed = Mathf.Abs((x * 73) + (z * 151) + ((x + z) * 19));

            if (rareAccentTileResourcePaths != null && rareAccentTileResourcePaths.Length > 0 && seed % 19 == 0)
            {
                return rareAccentTileResourcePaths[seed % rareAccentTileResourcePaths.Length];
            }

            if (mediumAccentTileResourcePaths != null && mediumAccentTileResourcePaths.Length > 0 && seed % 7 == 0)
            {
                return mediumAccentTileResourcePaths[seed % mediumAccentTileResourcePaths.Length];
            }

            if (baseTileResourcePaths == null || baseTileResourcePaths.Length == 0)
            {
                return null;
            }

            return baseTileResourcePaths[seed % baseTileResourcePaths.Length];
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
    }
}
