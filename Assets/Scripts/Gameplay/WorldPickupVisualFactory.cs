using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Gameplay
{
    public static class WorldPickupVisualFactory
    {
        private static Sprite cachedOrbSprite;

        public static void Create(
            Transform parent,
            Camera camera,
            string resourcePath,
            Color color,
            Vector3 localScale,
            Vector3 localPosition,
            int sortingOrder = 14,
            float pixelsPerUnit = 256f)
        {
            if (parent == null)
            {
                return;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"Missing pickup texture at Resources/{resourcePath}");
                return;
            }

            texture.filterMode = FilterMode.Point;

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(parent, false);
            visuals.transform.localPosition = localPosition;
            visuals.transform.localScale = localScale;

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(camera, BillboardSprite.BillboardMode.YAxisOnly);
        }

        public static void CreateOrb(
            Transform parent,
            Camera camera,
            Color coreColor,
            Color midColor,
            Color rimColor,
            Vector3 localScale,
            Vector3 localPosition,
            int sortingOrder = 14,
            float pixelsPerUnit = 256f)
        {
            if (parent == null)
            {
                return;
            }

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(parent, false);
            visuals.transform.localPosition = localPosition;
            visuals.transform.localScale = localScale;

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = GetOrCreateOrbSprite(coreColor, midColor, rimColor, pixelsPerUnit);
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(camera, BillboardSprite.BillboardMode.YAxisOnly);
        }

        private static Sprite GetOrCreateOrbSprite(Color coreColor, Color midColor, Color rimColor, float pixelsPerUnit)
        {
            if (cachedOrbSprite != null)
            {
                return cachedOrbSprite;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var radius = size * 0.38f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var point = new Vector2(x, y);
                    var distance = Vector2.Distance(point, center) / radius;
                    if (distance > 1f)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    var color = Color.Lerp(coreColor, midColor, Mathf.Clamp01(distance * 1.2f));
                    color = Color.Lerp(color, rimColor, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((distance - 0.58f) / 0.42f)));

                    var highlightDistance = Vector2.Distance(point, center + new Vector2(-10f, 10f)) / (radius * 0.75f);
                    var highlight = Mathf.Clamp01(1f - highlightDistance);
                    color = Color.Lerp(color, Color.Lerp(rimColor, Color.white, 0.35f), highlight * 0.45f);

                    var alpha = Mathf.SmoothStep(0f, 1f, 1f - distance);
                    color.a = Mathf.Lerp(0.72f, 1f, alpha);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            cachedOrbSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            return cachedOrbSprite;
        }
    }
}
