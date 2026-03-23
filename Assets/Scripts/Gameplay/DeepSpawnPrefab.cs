using Dagon.Core;
using Dagon.Rendering;
using Dagon.UI;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DeepSpawnPrefab : MonoBehaviour
    {
        private const float HealthPickupDropChance = 0.5f;
        private const float HealthPickupHealAmount = 2f;
        private const float HurtboxHeightLeniencyMultiplier = 1.3f;

        [Header("Stats")]
        [SerializeField] private float maxHealth = 24f;
        [SerializeField] private float driftSpeed = 1.2f;
        [SerializeField] private float chargeSpeed = 4.8f;
        [SerializeField] private float contactDamageAmount = 3f;
        [SerializeField] private int experienceReward = 6;
        [SerializeField] private float corruptionReward = 4f;

        [Header("Collision")]
        [SerializeField] private Vector3 colliderCenter = new(0f, 0.8f, 0f);
        [SerializeField] private float colliderHeight = 1.6f;
        [SerializeField] private float colliderRadius = 0.5f;

        [Header("Visuals")]
        [SerializeField] private string spriteResourcePath = "Sprites/Enemies/deep_spawn";
        [SerializeField] private float spritePixelsPerUnit = 64f;
        [SerializeField] private int sortingOrder = 7;
        [SerializeField] private Vector3 visualScale = new(0.9f, 0.9f, 1f);

        private Health health;
        private EnemyDeathRewards rewards;
        private ContactDamage contactDamage;
        private DeepSpawnBruiser bruiser;
        private Hurtbox hurtbox;
        private KnockbackReceiver knockbackReceiver;
        private BodyBlocker bodyBlocker;
        private EnemyHealthBar healthBar;
        private BillboardSprite billboard;
        private SpriteRenderer spriteRenderer;
        private Transform visualsRoot;

        public Health HealthComponent
        {
            get
            {
                EnsureSetup();
                return health;
            }
        }

        private void Awake()
        {
            EnsureSetup();
        }

        public void Configure(Transform target, Camera worldCamera, bool healthBarsAlwaysVisible = true, float visibleDurationAfterDamage = 2.25f)
        {
            EnsureSetup();

            health.SetMaxHealth(maxHealth, true);
            contactDamage.Configure(contactDamageAmount);
            rewards.Configure(experienceReward, corruptionReward, HealthPickupDropChance, HealthPickupHealAmount);
            bruiser.Configure(target, driftSpeed, chargeSpeed);
            billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);
            healthBar.Configure(worldCamera, new Vector3(0f, 1.8f, 0f), !healthBarsAlwaysVisible, visibleDurationAfterDamage);
        }

        public void ApplyCorruptionModifiers(float healthMultiplier, float damageMultiplier, float speedMultiplier, float cadenceMultiplier)
        {
            EnsureSetup();
            health.SetMaxHealth(maxHealth * Mathf.Max(0.1f, healthMultiplier), true);
            contactDamage.Configure(contactDamageAmount * Mathf.Max(0.1f, damageMultiplier));
            bruiser.Configure(
                null,
                driftSpeed * Mathf.Max(0.1f, speedMultiplier),
                chargeSpeed * Mathf.Max(0.1f, speedMultiplier * Mathf.Max(0.1f, cadenceMultiplier)));
        }

        private void EnsureSetup()
        {
            health = GetOrAddComponent(health);
            rewards = GetOrAddComponent(rewards);
            contactDamage = GetOrAddComponent(contactDamage);
            bruiser = GetOrAddComponent(bruiser);
            hurtbox = GetOrAddComponent(hurtbox);
            knockbackReceiver = GetOrAddComponent(knockbackReceiver);
            bodyBlocker = GetOrAddComponent(bodyBlocker);
            healthBar = GetOrAddComponent(healthBar);

            var capsuleCollider = GetOrAddComponent<CapsuleCollider>(null);
            capsuleCollider.center = colliderCenter;
            capsuleCollider.height = colliderHeight;
            capsuleCollider.radius = colliderRadius;
            capsuleCollider.isTrigger = true;
            ApplyVerticalHurtboxLeniency(capsuleCollider);

            var body = GetOrAddComponent<Rigidbody>(null);
            body.isKinematic = true;
            body.useGravity = false;

            hurtbox.Configure(CombatTeam.Enemy, health);
            knockbackReceiver.Configure(0.45f, 20f, 4.5f);
            bodyBlocker.Configure(BodyBlocker.BodyTeam.Enemy, 0.62f, colliderHeight, 1.8f, true, true);

            if (visualsRoot == null)
            {
                var existingVisuals = transform.Find("Visuals");
                if (existingVisuals != null)
                {
                    visualsRoot = existingVisuals;
                }
                else
                {
                    var visuals = new GameObject("Visuals");
                    visuals.transform.SetParent(transform, false);
                    visualsRoot = visuals.transform;
                }
            }

            visualsRoot.localPosition = Vector3.zero;
            visualsRoot.localScale = visualScale;

            spriteRenderer = GetOrAddComponent(spriteRenderer, visualsRoot.gameObject);
            spriteRenderer.sprite = RuntimeSpriteLibrary.LoadSprite(spriteResourcePath, spritePixelsPerUnit);
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.color = Color.white;

            billboard = GetOrAddComponent(billboard, visualsRoot.gameObject);
        }

        private T GetOrAddComponent<T>(T existing, GameObject owner = null) where T : Component
        {
            if (existing != null)
            {
                return existing;
            }

            owner ??= gameObject;
            if (owner.TryGetComponent<T>(out var component))
            {
                return component;
            }

            return owner.AddComponent<T>();
        }

        private static void ApplyVerticalHurtboxLeniency(CapsuleCollider collider)
        {
            var originalHeight = Mathf.Max(collider.radius * 2f, collider.height);
            var expandedHeight = Mathf.Max(originalHeight, originalHeight * HurtboxHeightLeniencyMultiplier);
            var extraHeight = expandedHeight - originalHeight;
            collider.height = expandedHeight;
            collider.center += new Vector3(0f, extraHeight * 0.5f, 0f);
        }
    }
}
