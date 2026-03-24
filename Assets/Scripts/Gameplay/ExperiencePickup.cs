using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ExperiencePickup : MonoBehaviour
    {
        private const string PickupSpriteResourcePath = "Sprites/Pickups/barnacle_shard";
        private const float DefaultLifetime = 20f;

        [SerializeField] private int experienceValue = 1;
        [SerializeField] private float corruptionValue = 0.5f;
        [SerializeField] private float attractDistance = 3f;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float lifetime = DefaultLifetime;

        private ExperienceController experienceController;
        private CorruptionMeter corruptionMeter;
        private CorruptionRuntimeEffects corruptionEffects;
        private Transform player;
        private Vector3 basePosition;

        public static ExperiencePickup Create(Vector3 position, int xpValue, float corruptionReward, Camera camera)
        {
            var pickup = new GameObject("ExperiencePickup");
            pickup.transform.position = position + Vector3.up * 0.2f;

            var sphere = pickup.AddComponent<SphereCollider>();
            sphere.radius = 0.35f;
            sphere.isTrigger = true;

            var rigidbody = pickup.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            var component = pickup.AddComponent<ExperiencePickup>();
            component.experienceValue = xpValue;
            component.corruptionValue = corruptionReward;

            WorldPickupVisualFactory.Create(
                pickup.transform,
                camera,
                PickupSpriteResourcePath,
                Color.white,
                new Vector3(0.24f, 0.24f, 1f),
                Vector3.zero);

            return component;
        }

        private void Awake()
        {
            basePosition = transform.position;
            experienceController = FindObjectOfType<ExperienceController>();
            var playerObject = FindObjectOfType<PlayerMover>();
            player = playerObject != null ? playerObject.transform : null;
            corruptionMeter = playerObject != null ? playerObject.GetComponent<CorruptionMeter>() : null;
            corruptionEffects = playerObject != null ? playerObject.GetComponent<CorruptionRuntimeEffects>() : null;
        }

        private void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            var bob = Mathf.Sin(Time.time * 4f) * 0.08f;
            transform.position = new Vector3(transform.position.x, basePosition.y + bob, transform.position.z);

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

            var effectiveExperience = Mathf.Max(1, Mathf.RoundToInt(experienceValue * (corruptionEffects != null ? corruptionEffects.ExperiencePickupValueMultiplier : 1f)));
            experienceController?.AddExperience(effectiveExperience);
            corruptionMeter?.AddCorruption(corruptionValue);
            Destroy(gameObject);
        }
    }
}
