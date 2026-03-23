using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CorruptedVariantVisual : MonoBehaviour
    {
        private static readonly Color TintColor = new(0.52f, 0.92f, 0.68f, 1f);
        private static readonly Color OverlayColor = new(0.16f, 0.82f, 0.56f, 0.42f);

        [SerializeField] private SpriteRenderer primaryRenderer;
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private float pulseSpeed = 3.2f;

        private SpriteRenderer overlayRenderer;

        public bool IsCorrupted { get; private set; }

        private void LateUpdate()
        {
            if (!IsCorrupted || primaryRenderer == null || overlayRenderer == null)
            {
                return;
            }

            overlayRenderer.sprite = primaryRenderer.sprite;
            overlayRenderer.flipX = primaryRenderer.flipX;
            overlayRenderer.flipY = primaryRenderer.flipY;
            overlayRenderer.sortingOrder = primaryRenderer.sortingOrder + 1;

            var pulse = 0.65f + (Mathf.Sin(Time.time * pulseSpeed) * 0.2f);
            overlayRenderer.color = new Color(OverlayColor.r, OverlayColor.g, OverlayColor.b, Mathf.Clamp01(pulse));
        }

        public void Apply(SpriteRenderer renderer, Color sourceColor)
        {
            primaryRenderer = renderer;
            baseColor = sourceColor;
            IsCorrupted = primaryRenderer != null;
            if (!IsCorrupted)
            {
                return;
            }

            EnsureOverlayRenderer();
            primaryRenderer.color = Color.Lerp(baseColor, TintColor, 0.6f);
            overlayRenderer.enabled = true;
        }

        private void EnsureOverlayRenderer()
        {
            if (overlayRenderer != null)
            {
                return;
            }

            var overlay = transform.Find("CorruptionOverlay");
            if (overlay == null)
            {
                var overlayObject = new GameObject("CorruptionOverlay");
                overlayObject.transform.SetParent(transform, false);
                overlayObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);
                overlayObject.transform.localScale = Vector3.one * 1.08f;
                overlay = overlayObject.transform;
            }

            overlayRenderer = overlay.GetComponent<SpriteRenderer>();
            if (overlayRenderer == null)
            {
                overlayRenderer = overlay.gameObject.AddComponent<SpriteRenderer>();
            }

            overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            overlayRenderer.receiveShadows = false;
        }
    }
}
