using UnityEngine;

namespace Dagon.Gameplay
{
    public static class LineAttackOverlayVisual
    {
        private const string DefaultSpritePath = "Sprites/Effects/brine_surge";
        private const float DefaultPixelsPerUnit = 256f;

        public static void Spawn(
            string name,
            Vector3 start,
            Vector3 end,
            float width,
            float duration,
            Camera camera,
            Color fillTint,
            Color innerTint,
            int fillSortingOrder = 6,
            int innerSortingOrder = 7)
        {
            var planarDelta = end - start;
            planarDelta.y = 0f;
            var length = planarDelta.magnitude;
            if (length <= 0.05f)
            {
                return;
            }

            var midpoint = start + (planarDelta * 0.5f);
            var yaw = Mathf.Atan2(planarDelta.x, planarDelta.z) * Mathf.Rad2Deg;
            var safeWidth = Mathf.Max(0.15f, width);
            var safeLength = Mathf.Max(0.5f, length);
            var outlineThickness = Mathf.Clamp(safeWidth * 0.16f, 0.04f, 0.14f);

            PlaceholderWeaponVisual.Spawn(
                $"{name}Fill",
                midpoint + Vector3.up * 0.05f,
                new Vector3(safeWidth * 1.18f, safeLength, 1f),
                camera,
                fillTint,
                duration,
                1.03f,
                yaw,
                spritePath: DefaultSpritePath,
                pixelsPerUnit: DefaultPixelsPerUnit,
                sortingOrder: fillSortingOrder,
                groundPlane: true,
                spriteAnchorNormalized: new Vector2(0.5f, 0.5f));

            PlaceholderWeaponVisual.Spawn(
                $"{name}Inner",
                midpoint + Vector3.up * 0.06f,
                new Vector3(safeWidth * 0.72f, safeLength * 0.94f, 1f),
                camera,
                innerTint,
                duration,
                1.05f,
                yaw,
                spritePath: DefaultSpritePath,
                pixelsPerUnit: DefaultPixelsPerUnit,
                sortingOrder: innerSortingOrder,
                groundPlane: true,
                spriteAnchorNormalized: new Vector2(0.5f, 0.5f));

            var outlineColor = new Color(
                Mathf.Clamp01(fillTint.r * 1.35f + 0.18f),
                Mathf.Clamp01(fillTint.g * 1.28f + 0.18f),
                Mathf.Clamp01(fillTint.b * 1.28f + 0.18f),
                Mathf.Clamp01(Mathf.Max(fillTint.a, innerTint.a) + 0.48f));

            RectOutlineVisual.Spawn(
                midpoint,
                safeWidth,
                safeLength,
                0.07f,
                outlineThickness,
                yaw,
                outlineColor,
                duration,
                1.01f,
                innerSortingOrder + 1,
                $"{name}Outline");
        }
    }
}
