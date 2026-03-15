using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class HarpoonLauncher : MonoBehaviour
    {
        [SerializeField] private PlayerMover playerMover;
        [SerializeField] private HarpoonProjectile projectilePrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float attacksPerSecond = 2f;
        [SerializeField] private float projectileSpeed = 14f;
        [SerializeField] private float projectileDamage = 1f;
        [SerializeField] private int projectilesPerVolley = 1;
        [SerializeField] private float spreadAngle = 8f;
        [SerializeField] private bool autoFire = true;

        private float cooldownTimer;

        private void Awake()
        {
            if (playerMover == null)
            {
                playerMover = GetComponent<PlayerMover>();
            }
        }

        private void Update()
        {
            if (!autoFire || projectilePrefab == null || attacksPerSecond <= 0f)
            {
                return;
            }

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer > 0f)
            {
                return;
            }

            FireVolley();
            cooldownTimer = 1f / attacksPerSecond;
        }

        public void Configure(float newAttacksPerSecond, float newProjectileSpeed, float newProjectileDamage, int newProjectilesPerVolley, float newSpreadAngle)
        {
            attacksPerSecond = Mathf.Max(0.01f, newAttacksPerSecond);
            projectileSpeed = Mathf.Max(0.01f, newProjectileSpeed);
            projectileDamage = Mathf.Max(0.01f, newProjectileDamage);
            projectilesPerVolley = Mathf.Max(1, newProjectilesPerVolley);
            spreadAngle = Mathf.Max(0f, newSpreadAngle);
        }

        public void SetProjectilePrefab(HarpoonProjectile prefab)
        {
            projectilePrefab = prefab;
        }

        public void ModifyAttacksPerSecond(float amount)
        {
            attacksPerSecond = Mathf.Max(0.1f, attacksPerSecond + amount);
        }

        public void ModifyProjectileDamage(float amount)
        {
            projectileDamage = Mathf.Max(0.1f, projectileDamage + amount);
        }

        public void ModifyProjectileCount(int amount)
        {
            projectilesPerVolley = Mathf.Max(1, projectilesPerVolley + amount);
        }

        public void FireVolley()
        {
            var origin = spawnPoint != null ? spawnPoint.position : transform.position;
            var baseDirection = playerMover != null ? playerMover.AimDirection : transform.forward;
            if (baseDirection.sqrMagnitude < 0.001f)
            {
                baseDirection = transform.forward;
            }

            var count = Mathf.Max(1, projectilesPerVolley);
            var startAngle = -spreadAngle * 0.5f * (count - 1);

            for (var i = 0; i < count; i++)
            {
                var yaw = startAngle + (spreadAngle * i);
                var rotation = Quaternion.AngleAxis(yaw, Vector3.up);
                var direction = rotation * baseDirection.normalized;
                var projectile = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction, Vector3.up));
                projectile.gameObject.SetActive(true);
                projectile.Initialize(gameObject, direction, projectileSpeed, projectileDamage);
            }
        }
    }
}
