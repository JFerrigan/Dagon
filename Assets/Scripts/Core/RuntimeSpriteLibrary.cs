using UnityEngine;

namespace Dagon.Core
{
    public static class RuntimeSpriteLibrary
    {
        public static Sprite LoadSprite(string resourcePath, float pixelsPerUnit = 64f)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"Missing sprite texture at Resources/{resourcePath}");
                return null;
            }

            texture.filterMode = FilterMode.Point;
            var rect = new Rect(0f, 0f, texture.width, texture.height);
            var pivot = new Vector2(0.5f, 0f);
            return Sprite.Create(texture, rect, pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
        }
    }
}
