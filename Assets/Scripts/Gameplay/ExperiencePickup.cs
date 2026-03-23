using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ExperiencePickup : MonoBehaviour
    {
        private const string PickupSpriteResourcePath = "Sprites/Pickups/barnacle_shard";

        [SerializeField] private int experienceValue = 1;
        [SerializeField] private float corruptionValue = 0.5f;
        [SerializeField] private float attractDistance = 3f;
        [SerializeField] private float moveSpeed = 5f;

        private ExperienceController experienceController;
        private CorruptionMeter corruptionMeter;
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
        }

        private void Update()
        {
            var bob = Mathf.Sin(Time.time * 4f) * 0.08f;
            transform.position = new Vector3(transform.position.x, basePosition.y + bob, transform.position.z);

            if (player == null)
            {
                return;
            }

            var toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > attractDistance * attractDistance)
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

            experienceController?.AddExperience(experienceValue);
            corruptionMeter?.AddCorruption(corruptionValue);
            Destroy(gameObject);
        }
    }
}
