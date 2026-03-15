using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MireColossusController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private HarpoonProjectile orbProjectilePrefab;
        [SerializeField] private float driftSpeed = 1.05f;
        [SerializeField] private float burstCooldown = 4.5f;
        [SerializeField] private float burstProjectileSpeed = 4.5f;
        [SerializeField] private float burstProjectileDamage = 1f;
        [SerializeField] private int burstCount = 8;

        private float burstTimer;

        private void Update()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                target = player != null ? player.transform : null;
            }

            if (target == null)
            {
                return;
            }

            var toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.04f)
            {
                transform.position += toTarget.normalized * (driftSpeed * Time.deltaTime);
            }

            burstTimer -= Time.deltaTime;
            if (burstTimer > 0f || orbProjectilePrefab == null)
            {
                return;
            }

            FireBurst();
            burstTimer = burstCooldown;
        }

        public void Configure(Transform newTarget, HarpoonProjectile projectilePrefab)
        {
            target = newTarget;
            orbProjectilePrefab = projectilePrefab;
            burstTimer = 2.5f;
        }

        private void FireBurst()
        {
            for (var i = 0; i < burstCount; i++)
            {
                var angle = i * (360f / burstCount);
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var projectile = Instantiate(
                    orbProjectilePrefab,
                    transform.position + Vector3.up * 0.5f,
                    Quaternion.LookRotation(direction, Vector3.up));
                projectile.gameObject.SetActive(true);
                projectile.Initialize(gameObject, direction, burstProjectileSpeed, burstProjectileDamage);
            }
        }
    }
}
