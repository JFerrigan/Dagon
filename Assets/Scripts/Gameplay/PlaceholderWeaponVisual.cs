using Dagon.Core;
using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlaceholderWeaponVisual : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Transform scaleRoot;
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
            int sortingOrder = 15,
            bool groundPlane = false,
            Vector3? spriteLocalOffset = null)
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
            visual.Initialize(sprite, scale, camera, tint, duration, endScaleMultiplier, sortingOrder, groundPlane, spriteLocalOffset ?? Vector3.zero);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            var progress = Mathf.Clamp01(timer / duration);
            if (scaleRoot != null)
            {
                scaleRoot.localScale = Vector3.Lerp(startScale, endScale, progress);
            }

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
            int sortingOrder,
            bool groundPlane,
            Vector3 spriteLocalOffset)
        {
            duration = Mathf.Max(0.05f, visualDuration);
            tint = visualTint;
            startScale = ResolveAspectPreservingScale(sprite, scale);
            endScale = startScale * endScaleMultiplier;

            var offsetRoot = new GameObject("OffsetRoot");
            offsetRoot.transform.SetParent(transform, false);
            offsetRoot.transform.localPosition = spriteLocalOffset;

            var scaleObject = new GameObject("ScaleRoot");
            scaleObject.transform.SetParent(offsetRoot.transform, false);
            scaleRoot = scaleObject.transform;
            scaleRoot.localScale = startScale;

            var rendererObject = new GameObject("Visuals");
            rendererObject.transform.SetParent(scaleRoot, false);
            spriteRenderer = rendererObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.color = tint;

            if (groundPlane)
            {
                rendererObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                var billboard = rendererObject.AddComponent<BillboardSprite>();
                billboard.Configure(camera, BillboardSprite.BillboardMode.YAxisOnly);
            }
        }

        private static Vector3 ResolveAspectPreservingScale(Sprite sprite, Vector3 requestedScale)
        {
            if (sprite == null)
            {
                return requestedScale;
            }

            var spriteWidth = Mathf.Max(1f, sprite.rect.width);
            var spriteHeight = Mathf.Max(1f, sprite.rect.height);
            var spriteAspect = spriteWidth / spriteHeight;

            var maxWidth = Mathf.Max(0.0001f, requestedScale.x);
            var maxHeight = Mathf.Max(0.0001f, requestedScale.y);

            var resolvedWidth = maxWidth;
            var resolvedHeight = resolvedWidth / spriteAspect;

            if (resolvedHeight > maxHeight)
            {
                resolvedHeight = maxHeight;
                resolvedWidth = resolvedHeight * spriteAspect;
            }

            return new Vector3(resolvedWidth, resolvedHeight, requestedScale.z);
        }
    }
}
