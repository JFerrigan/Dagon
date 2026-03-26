using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CorruptionPickup : MonoBehaviour
    {
        private const float DefaultLifetime = 20f;
        private static readonly Color DefaultCoreColor = new(0.04f, 0.03f, 0.08f, 1f);
        private static readonly Color DefaultMidColor = new(0.28f, 0.14f, 0.42f, 1f);
        private static readonly Color DefaultRimColor = new(0.74f, 0.56f, 0.92f, 0.98f);
        private static readonly Color UndertideCoreColor = new(0.98f, 0.98f, 0.92f, 1f);
        private static readonly Color UndertideMidColor = new(0.98f, 0.94f, 0.58f, 1f);
        private static readonly Color UndertideRimColor = new(1f, 0.90f, 0.42f, 0.98f);

        [SerializeField] private float corruptionValue = 2f;
        [SerializeField] private float attractDistance = 3f;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float lifetime = DefaultLifetime;

        private CorruptionMeter corruptionMeter;
        private CorruptionRuntimeEffects corruptionEffects;
        private WorldProgressionDirector worldProgressionDirector;
        private SpriteRenderer orbRenderer;
        private Transform player;
        private Vector3 basePosition;
        private bool undertideReversed;

        public static CorruptionPickup Create(Vector3 position, float corruptionAmount, Camera camera)
        {
            var pickup = new GameObject("CorruptionPickup");
            pickup.transform.position = position + Vector3.up * 0.2f;

            var sphere = pickup.AddComponent<SphereCollider>();
            sphere.radius = 0.35f;
            sphere.isTrigger = true;

            var rigidbody = pickup.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            var component = pickup.AddComponent<CorruptionPickup>();
            component.corruptionValue = corruptionAmount;
            component.orbRenderer = WorldPickupVisualFactory.CreateOrb(
                pickup.transform,
                camera,
                DefaultCoreColor,
                DefaultMidColor,
                DefaultRimColor,
                new Vector3(0.68f, 0.68f, 1f),
                Vector3.zero,
                sortingOrder: 16);

            return component;
        }

        private void Awake()
        {
            basePosition = transform.position;
            var playerObject = FindObjectOfType<PlayerMover>();
            player = playerObject != null ? playerObject.transform : null;
            corruptionMeter = playerObject != null ? playerObject.GetComponent<CorruptionMeter>() : null;
            corruptionEffects = playerObject != null ? playerObject.GetComponent<CorruptionRuntimeEffects>() : null;
            worldProgressionDirector = FindFirstObjectByType<WorldProgressionDirector>();
        }

        private void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            var bob = Mathf.Sin(Time.time * 4.6f) * 0.08f;
            transform.position = new Vector3(transform.position.x, basePosition.y + bob, transform.position.z);
            RefreshUndertideState();

            if (player == null)
            {
                return;
            }

            var toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            var effectiveAttractDistance = attractDistance * (corruptionEffects != null ? corruptionEffects.PickupAttractRadiusMultiplier : 1f);
            if (toPlayer.sqrMagnitude > effectiveAttractDistance * effectiveAttractDistance)
            {
                return;
            }

            transform.position += toPlayer.normalized * (moveSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (undertideReversed)
            {
                corruptionMeter?.ReduceCorruption(corruptionValue);
            }
            else
            {
                corruptionMeter?.AddCorruption(corruptionValue);
            }

            Destroy(gameObject);
        }

        private void RefreshUndertideState()
        {
            worldProgressionDirector ??= FindFirstObjectByType<WorldProgressionDirector>();
            var nextUndertideReversed =
                corruptionEffects != null &&
                corruptionEffects.HasUndertide &&
                worldProgressionDirector != null &&
                worldProgressionDirector.IsPositionCorrupted(transform.position);
            if (nextUndertideReversed == undertideReversed)
            {
                return;
            }

            undertideReversed = nextUndertideReversed;
            WorldPickupVisualFactory.UpdateOrb(
                orbRenderer,
                undertideReversed ? UndertideCoreColor : DefaultCoreColor,
                undertideReversed ? UndertideMidColor : DefaultMidColor,
                undertideReversed ? UndertideRimColor : DefaultRimColor);
        }
    }
}
