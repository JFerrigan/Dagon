using Dagon.Core;
using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BrineSurgeVisual : MonoBehaviour
    {
        [SerializeField] private string spriteResourcePath = "Sprites/Effects/brine_surge";
        [SerializeField] private float spritePixelsPerUnit = 256f;
        [SerializeField] private float duration = 0.45f;
        [SerializeField] private float endScaleMultiplier = 1.35f;
        [SerializeField] private int sortingOrder = 16;
        [SerializeField] private Color tint = new(0.92f, 1f, 0.92f, 0.9f);

        public static void Spawn(Vector3 position, float radius, Camera worldCamera)
        {
            var sprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Effects/brine_surge", 256f);
            if (sprite == null)
            {
                return;
            }

            var effect = new GameObject("BrineSurgeVisual");
            effect.transform.position = position + Vector3.up * 0.15f;

            var visual = effect.AddComponent<BrineSurgeVisual>();
            visual.Initialize(sprite, radius, worldCamera);
        }

        private SpriteRenderer spriteRenderer;
        private BillboardSprite billboard;
        private float timer;
        private Vector3 startScale;
        private Vector3 endScale;

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

        private void Initialize(Sprite sprite, float radius, Camera worldCamera)
        {
            var rendererObject = new GameObject("Visuals");
            rendererObject.transform.SetParent(transform, false);

            spriteRenderer = rendererObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.color = tint;

            billboard = rendererObject.AddComponent<BillboardSprite>();
            billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);

            var diameter = Mathf.Max(1f, radius * 2f);
            startScale = new Vector3(diameter, diameter, 1f);
            endScale = startScale * endScaleMultiplier;
            transform.localScale = startScale;
        }
    }
}
