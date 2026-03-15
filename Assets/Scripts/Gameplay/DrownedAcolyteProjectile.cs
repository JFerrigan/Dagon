using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DrownedAcolyteProjectile : MonoBehaviour
    {
        [SerializeField] private float lifetime = 2.2f;

        private GameObject owner;
        private Vector3 direction = Vector3.forward;
        private float speed = 6.5f;
        private float impactDamage = 1f;
        private float hazardRadius = 1.2f;
        private float hazardDuration = 2.25f;
        private float hazardTickDamage = 0.5f;
        private float hazardTickInterval = 0.5f;
        private Camera worldCamera;

        public void Initialize(
            GameObject projectileOwner,
            Vector3 moveDirection,
            float moveSpeed,
            float directDamage,
            float zoneRadius,
            float zoneDuration,
            float zoneTickDamage,
            float zoneTickInterval,
            Camera cameraReference)
        {
            owner = projectileOwner;
            direction = moveDirection.normalized;
            speed = moveSpeed;
            impactDamage = directDamage;
            hazardRadius = zoneRadius;
            hazardDuration = zoneDuration;
            hazardTickDamage = zoneTickDamage;
            hazardTickInterval = zoneTickInterval;
            worldCamera = cameraReference;
        }

        private void Update()
        {
            transform.position += direction * (speed * Time.deltaTime);
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Explode();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == owner || (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == owner))
            {
                return;
            }

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && (other.CompareTag("Player") || other.GetComponentInParent<PlayerMover>() != null))
            {
                damageable.ApplyDamage(impactDamage, owner);
            }

            Explode();
        }

        private void Explode()
        {
            EnemyHazardZone.Spawn(
                transform.position,
                hazardRadius,
                hazardDuration,
                hazardTickDamage,
                hazardTickInterval,
                worldCamera,
                new Color(0.38f, 0.82f, 0.52f, 0.48f),
                "AcolyteHazardZone");
            Destroy(gameObject);
        }
    }
}
