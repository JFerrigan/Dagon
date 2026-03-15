using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DrownedAcolyteShooter : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private DrownedAcolyteProjectile projectilePrefab;
        [SerializeField] private float moveSpeed = 2.6f;
        [SerializeField] private float preferredRange = 6f;
        [SerializeField] private float fireCooldown = 1.6f;
        [SerializeField] private float projectileSpeed = 6.5f;
        [SerializeField] private float projectileDamage = 1f;
        [SerializeField] private float hazardRadius = 1.2f;
        [SerializeField] private float hazardDuration = 2.25f;
        [SerializeField] private float hazardTickDamage = 0.5f;
        [SerializeField] private float hazardTickInterval = 0.5f;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private EnemySlowReceiver slowReceiver;

        private float fireTimer;

        private void Awake()
        {
            if (slowReceiver == null)
            {
                slowReceiver = GetComponent<EnemySlowReceiver>();
            }
        }

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

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            var toTarget = target.position - transform.position;
            toTarget.y = 0f;
            var distance = toTarget.magnitude;
            var effectiveSpeed = moveSpeed * (slowReceiver != null ? slowReceiver.SpeedMultiplier : 1f);

            if (distance > preferredRange + 0.75f)
            {
                transform.position += toTarget.normalized * (effectiveSpeed * Time.deltaTime);
            }
            else if (distance < preferredRange - 0.65f && distance > 0.01f)
            {
                transform.position -= toTarget.normalized * (effectiveSpeed * Time.deltaTime);
            }

            fireTimer -= Time.deltaTime;
            if (fireTimer > 0f || projectilePrefab == null || distance <= 0.25f)
            {
                return;
            }

            var direction = toTarget.normalized;
            var projectile = Instantiate(
                projectilePrefab,
                transform.position + Vector3.up * 0.38f,
                Quaternion.LookRotation(direction, Vector3.up));
            projectile.gameObject.SetActive(true);
            projectile.Initialize(
                gameObject,
                direction,
                projectileSpeed,
                projectileDamage,
                hazardRadius,
                hazardDuration,
                hazardTickDamage,
                hazardTickInterval,
                worldCamera);
            fireTimer = fireCooldown;
        }

        public void Configure(
            Transform newTarget,
            DrownedAcolyteProjectile prefab,
            float newMoveSpeed,
            float newPreferredRange,
            float newCooldown,
            Camera cameraReference)
        {
            target = newTarget;
            projectilePrefab = prefab;
            moveSpeed = Mathf.Max(0.1f, newMoveSpeed);
            preferredRange = Mathf.Max(1f, newPreferredRange);
            fireCooldown = Mathf.Max(0.2f, newCooldown);
            worldCamera = cameraReference;
        }
    }
}
