using Dagon.Core;
using Dagon.Rendering;
using UnityEngine;

namespace Dagon.UI
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        private static Sprite sharedBarSprite;

        [SerializeField] private Health health;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Vector3 localOffset = new(0f, 1.55f, 0f);
        [SerializeField] private Vector2 barSize = new(1.1f, 0.14f);
        [SerializeField] private bool hideWhenFull = true;
        [SerializeField] private float visibleDurationAfterDamage = 2.25f;

        private Transform barRoot;
        private Transform backgroundTransform;
        private Transform fillTransform;
        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer fillRenderer;
        private BillboardSprite billboard;
        private float visibleUntilTime;

        public bool HideWhenFull => hideWhenFull;
        public float VisibleDurationAfterDamage => visibleDurationAfterDamage;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            EnsureVisuals();
            RefreshVisibility(true);
        }

        private void OnEnable()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (health != null)
            {
                health.Changed += HandleHealthChanged;
                health.Died += HandleDied;
            }

            RefreshVisualState();
            RefreshVisibility(true);
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Changed -= HandleHealthChanged;
                health.Died -= HandleDied;
            }
        }

        private void LateUpdate()
        {
            if (barRoot == null)
            {
                return;
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
                if (billboard != null && worldCamera != null)
                {
                    billboard.Configure(worldCamera, BillboardSprite.BillboardMode.Full);
                }
            }

            barRoot.localPosition = localOffset;

            if (!hideWhenFull || health == null || health.IsDead)
            {
                return;
            }

            barRoot.gameObject.SetActive(Time.time <= visibleUntilTime && !IsAtFullHealth());
        }

        public void Configure(Camera cameraReference, Vector3 offset, bool hideAtFullHealth = true, float damageVisibilityDuration = 2.25f)
        {
            worldCamera = cameraReference;
            localOffset = offset;
            hideWhenFull = hideAtFullHealth;
            visibleDurationAfterDamage = Mathf.Max(0.1f, damageVisibilityDuration);

            EnsureVisuals();
            if (billboard != null && worldCamera != null)
            {
                billboard.Configure(worldCamera, BillboardSprite.BillboardMode.Full);
            }

            RefreshVisualState();
            RefreshVisibility(true);
        }

        private void HandleHealthChanged(Health changedHealth)
        {
            RefreshVisualState();

            if (hideWhenFull && changedHealth != null && !changedHealth.IsDead && !IsAtFullHealth())
            {
                visibleUntilTime = Time.time + visibleDurationAfterDamage;
            }

            RefreshVisibility(false);
        }

        private void HandleDied(Health deadHealth, GameObject source)
        {
            if (barRoot != null)
            {
                barRoot.gameObject.SetActive(false);
            }
        }

        private void EnsureVisuals()
        {
            if (barRoot == null)
            {
                var existing = transform.Find("EnemyHealthBar");
                if (existing != null)
                {
                    barRoot = existing;
                }
                else
                {
                    var root = new GameObject("EnemyHealthBar");
                    root.transform.SetParent(transform, false);
                    barRoot = root.transform;
                }
            }

            barRoot.localPosition = localOffset;
            barRoot.localRotation = Quaternion.identity;
            barRoot.localScale = Vector3.one;

            if (sharedBarSprite == null)
            {
                sharedBarSprite = CreateBarSprite();
            }

            EnsureChildRenderer("Background", ref backgroundTransform, ref backgroundRenderer);
            EnsureChildRenderer("Fill", ref fillTransform, ref fillRenderer);

            backgroundRenderer.sprite = sharedBarSprite;
            backgroundRenderer.sortingOrder = 60;
            backgroundRenderer.color = new Color(0.06f, 0.08f, 0.07f, 0.95f);

            fillRenderer.sprite = sharedBarSprite;
            fillRenderer.sortingOrder = 61;
            fillRenderer.color = new Color(0.84f, 0.16f, 0.14f, 0.98f);

            billboard = barRoot.GetComponent<BillboardSprite>() ?? barRoot.gameObject.AddComponent<BillboardSprite>();
            if (worldCamera != null)
            {
                billboard.Configure(worldCamera, BillboardSprite.BillboardMode.Full);
            }
        }

        private void RefreshVisualState()
        {
            if (health == null || backgroundRenderer == null || fillRenderer == null || fillTransform == null)
            {
                return;
            }

            var normalized = health.MaxHealth > 0f
                ? Mathf.Clamp01(health.CurrentHealth / health.MaxHealth)
                : 0f;

            if (backgroundTransform != null)
            {
                backgroundTransform.localScale = new Vector3(barSize.x, barSize.y, 1f);
                backgroundTransform.localPosition = Vector3.zero;
            }

            var fillWidth = Mathf.Max(0.0001f, barSize.x * normalized);
            fillTransform.localScale = new Vector3(fillWidth, barSize.y * 0.72f, 1f);
            fillTransform.localPosition = new Vector3((-barSize.x * 0.5f) + (fillWidth * 0.5f), 0f, -0.01f);
        }

        private void RefreshVisibility(bool forceImmediateVisibleWindow)
        {
            if (barRoot == null)
            {
                return;
            }

            if (health == null || health.IsDead)
            {
                barRoot.gameObject.SetActive(false);
                return;
            }

            if (!hideWhenFull)
            {
                barRoot.gameObject.SetActive(true);
                return;
            }

            if (forceImmediateVisibleWindow && !IsAtFullHealth())
            {
                visibleUntilTime = Mathf.Max(visibleUntilTime, Time.time + visibleDurationAfterDamage);
            }

            barRoot.gameObject.SetActive(Time.time <= visibleUntilTime && !IsAtFullHealth());
        }

        private bool IsAtFullHealth()
        {
            return health == null || health.MaxHealth <= 0f || health.CurrentHealth >= health.MaxHealth;
        }

        private Transform GetOrCreateChild(string childName)
        {
            var child = barRoot.Find(childName);
            if (child != null)
            {
                return child;
            }

            var childObject = new GameObject(childName);
            childObject.transform.SetParent(barRoot, false);
            return childObject.transform;
        }

        private void EnsureChildRenderer(string childName, ref Transform childTransform, ref SpriteRenderer renderer)
        {
            if (childTransform == null)
            {
                childTransform = GetOrCreateChild(childName);
            }

            if (renderer == null || renderer.gameObject != childTransform.gameObject)
            {
                renderer = childTransform.GetComponent<SpriteRenderer>();
            }

            if (renderer == null)
            {
                renderer = childTransform.gameObject.AddComponent<SpriteRenderer>();
            }
        }

        private static Sprite CreateBarSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
