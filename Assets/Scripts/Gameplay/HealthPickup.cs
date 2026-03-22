using Dagon.Core;
using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class HealthPickup : MonoBehaviour
    {
        private const string PickupSpriteResourcePath = "Sprites/UI/heart";

        [SerializeField] private float healAmount = 2f;
        [SerializeField] private float attractDistance = 3f;
        [SerializeField] private float moveSpeed = 5f;

        private Health playerHealth;
        private Transform player;
        private Vector3 basePosition;

        public static HealthPickup Create(Vector3 position, float healValue, Camera camera)
        {
            var pickup = new GameObject("HealthPickup");
            pickup.transform.position = position + Vector3.up * 0.2f;

            var sphere = pickup.AddComponent<SphereCollider>();
            sphere.radius = 0.35f;
            sphere.isTrigger = true;

            var rigidbody = pickup.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            var component = pickup.AddComponent<HealthPickup>();
            component.healAmount = Mathf.Max(0.1f, healValue);

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(pickup.transform, false);

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteLibrary.LoadSprite(PickupSpriteResourcePath, 256f);
            renderer.color = new Color(1f, 0.92f, 0.92f, 1f);
            renderer.sortingOrder = 14;
            visuals.transform.localScale = new Vector3(0.14f, 0.14f, 1f);

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(camera, BillboardSprite.BillboardMode.YAxisOnly);

            return component;
        }

        private void Awake()
        {
            basePosition = transform.position;
            var playerObject = FindObjectOfType<PlayerMover>();
            player = playerObject != null ? playerObject.transform : null;
            playerHealth = playerObject != null ? playerObject.GetComponent<Health>() : null;
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

            playerHealth?.Restore(healAmount);
            Destroy(gameObject);
        }
    }
}
