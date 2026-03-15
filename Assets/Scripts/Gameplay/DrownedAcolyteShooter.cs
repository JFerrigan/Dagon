using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DrownedAcolyteShooter : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private HarpoonProjectile projectilePrefab;
        [SerializeField] private float moveSpeed = 1.2f;
        [SerializeField] private float preferredRange = 5.5f;
        [SerializeField] private float fireCooldown = 2.4f;
        [SerializeField] private float projectileSpeed = 5.5f;
        [SerializeField] private float projectileDamage = 1f;

        private float fireTimer;

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
            var distance = toTarget.magnitude;

            if (distance > preferredRange + 0.6f)
            {
                transform.position += toTarget.normalized * (moveSpeed * Time.deltaTime);
            }
            else if (distance < preferredRange - 0.8f && distance > 0.01f)
            {
                transform.position -= toTarget.normalized * (moveSpeed * Time.deltaTime);
            }

            fireTimer -= Time.deltaTime;
            if (fireTimer > 0f || projectilePrefab == null || distance <= 0.2f)
            {
                return;
            }

            var direction = toTarget.normalized;
            var projectile = Object.Instantiate(projectilePrefab, transform.position + Vector3.up * 0.3f, Quaternion.LookRotation(direction, Vector3.up));
            projectile.gameObject.SetActive(true);
            projectile.Initialize(gameObject, direction, projectileSpeed, projectileDamage);
            fireTimer = fireCooldown;
        }

        public void Configure(Transform newTarget, HarpoonProjectile prefab, float newMoveSpeed, float newPreferredRange, float newCooldown)
        {
            target = newTarget;
            projectilePrefab = prefab;
            moveSpeed = Mathf.Max(0.1f, newMoveSpeed);
            preferredRange = Mathf.Max(1f, newPreferredRange);
            fireCooldown = Mathf.Max(0.2f, newCooldown);
        }
    }
}
