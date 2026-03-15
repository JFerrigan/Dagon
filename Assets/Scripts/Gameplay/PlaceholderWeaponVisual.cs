using Dagon.Core;
using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlaceholderWeaponVisual : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float duration;
        private float timer;
        private Color tint;
        private Vector3 startScale;
        private Vector3 endScale;

        public static void Spawn(
            string name,
            Vector3 position,
            Vector3 scale,
            Camera camera,
            Color tint,
            float duration = 0.28f,
            float endScaleMultiplier = 1.15f,
            float yaw = 0f,
            string spritePath = "Sprites/Effects/brine_surge",
            float pixelsPerUnit = 256f,
            int sortingOrder = 15)
        {
            var sprite = RuntimeSpriteLibrary.LoadSprite(spritePath, pixelsPerUnit);
            if (sprite == null)
            {
                return;
            }

            var effect = new GameObject(name);
            effect.transform.position = position;
            effect.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var visual = effect.AddComponent<PlaceholderWeaponVisual>();
            visual.Initialize(sprite, scale, camera, tint, duration, endScaleMultiplier, sortingOrder);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            var progress = Mathf.Clamp01(timer / duration);
            transform.localScale = Vector3.Lerp(startScale, endScale, progress);

            if (spriteRenderer != null)
            {
                var color = tint;
                color.a *= 1f - progress;
                spriteRenderer.color = color;
            }

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void Initialize(
            Sprite sprite,
            Vector3 scale,
            Camera camera,
            Color visualTint,
            float visualDuration,
            float endScaleMultiplier,
            int sortingOrder)
        {
            duration = Mathf.Max(0.05f, visualDuration);
            tint = visualTint;
            startScale = scale;
            endScale = scale * endScaleMultiplier;
            transform.localScale = startScale;

            var rendererObject = new GameObject("Visuals");
            rendererObject.transform.SetParent(transform, false);
            spriteRenderer = rendererObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.color = tint;

            var billboard = rendererObject.AddComponent<BillboardSprite>();
            billboard.Configure(camera, BillboardSprite.BillboardMode.YAxisOnly);
        }
    }
}
