using Dagon.Core;
using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DeepSpawnPrefab : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 20f;
        [SerializeField] private float driftSpeed = 1.15f;
        [SerializeField] private float chargeSpeed = 4.4f;
        [SerializeField] private float contactDamageAmount = 3f;
        [SerializeField] private int experienceReward = 6;
        [SerializeField] private float corruptionReward = 7f;

        [Header("Collision")]
        [SerializeField] private Vector3 colliderCenter = new(0f, 0.75f, 0f);
        [SerializeField] private float colliderHeight = 1.5f;
        [SerializeField] private float colliderRadius = 0.35f;

        [Header("Visuals")]
        [SerializeField] private string spriteResourcePath = "Sprites/Enemies/deep_spawn";
        [SerializeField] private float spritePixelsPerUnit = 256f;
        [SerializeField] private int sortingOrder = 7;
        [SerializeField] private Vector3 visualScale = new(1.2f, 1.2f, 1f);

        private Health health;
        private EnemyDeathRewards rewards;
        private ContactDamage contactDamage;
        private DeepSpawnBruiser bruiser;
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

        public void Configure(Transform target, Camera worldCamera)
        {
            EnsureSetup();

            health.SetMaxHealth(maxHealth, true);
            contactDamage.Configure(contactDamageAmount);
            rewards.Configure(experienceReward, corruptionReward);
            bruiser.Configure(target, driftSpeed, chargeSpeed);
            billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);
        }

        private void EnsureSetup()
        {
            health = GetOrAddComponent(health);
            rewards = GetOrAddComponent(rewards);
            contactDamage = GetOrAddComponent(contactDamage);
            bruiser = GetOrAddComponent(bruiser);

            var capsuleCollider = GetOrAddComponent<CapsuleCollider>(null);
            capsuleCollider.center = colliderCenter;
            capsuleCollider.height = colliderHeight;
            capsuleCollider.radius = colliderRadius;
            capsuleCollider.isTrigger = true;

            var body = GetOrAddComponent<Rigidbody>(null);
            body.isKinematic = true;
            body.useGravity = false;

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
    }
}
