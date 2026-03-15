using Dagon.Core;
using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ExperiencePickup : MonoBehaviour
    {
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

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(pickup.transform, false);

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Weapons/harpoon_projectile");
            renderer.color = new Color(0.62f, 0.92f, 0.66f, 0.95f);
            renderer.sortingOrder = 14;
            visuals.transform.localScale = new Vector3(0.02f, 0.02f, 1f);

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(camera, BillboardSprite.BillboardMode.YAxisOnly);

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
