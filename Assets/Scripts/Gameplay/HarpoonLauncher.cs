using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    public sealed class HarpoonLauncher : PlayerWeaponRuntime
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
        [SerializeField] private WeaponProjectileVisualKind projectileVisualKind = WeaponProjectileVisualKind.Harpoon;

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

        public override void ConfigureRuntime(Camera worldCamera)
        {
            SetProjectilePrefab(CreateProjectilePrefab(worldCamera));
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

        public override void ModifyAttackRate(float amount)
        {
            attacksPerSecond = Mathf.Max(0.1f, attacksPerSecond + amount);
        }

        public override void ModifyProjectileDamage(float amount)
        {
            projectileDamage = Mathf.Max(0.1f, projectileDamage + amount);
        }

        public override void ModifyProjectileCount(int amount)
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

        protected override void ApplyDefinition(WeaponDefinition runtimeDefinition)
        {
            projectileVisualKind = runtimeDefinition.ProjectileVisualKind;
            Configure(
                runtimeDefinition.AttacksPerSecond,
                runtimeDefinition.ProjectileSpeed,
                runtimeDefinition.ProjectileDamage,
                runtimeDefinition.ProjectilesPerVolley,
                runtimeDefinition.SpreadAngle);
        }

        protected override void ApplyRankBonus(int currentRank)
        {
            ModifyProjectileDamage(0.35f);
            if (currentRank % 2 == 0)
            {
                ModifyAttackRate(0.12f);
            }
        }

        private HarpoonProjectile CreateProjectilePrefab(Camera worldCamera)
        {
            return projectileVisualKind switch
            {
                WeaponProjectileVisualKind.Orb => RuntimeOrbProjectileFactory.Create(worldCamera),
                _ => RuntimeHarpoonProjectileFactory.Create(worldCamera)
            };
        }
    }
}
