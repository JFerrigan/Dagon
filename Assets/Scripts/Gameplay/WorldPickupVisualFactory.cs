using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Gameplay
{
    public static class WorldPickupVisualFactory
    {
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
    }
}
