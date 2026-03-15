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

        public override string PathAName => "Volley Path";
        public override string PathBName => "Heavy Harpoon Path";

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
            spreadAngle = projectilesPerVolley switch
            {
                1 => 0f,
                2 => 8f,
                3 => 14f,
                _ => 20f
            };
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

        protected override void ApplyPathUpgrade(WeaponUpgradePath path, int nextStep)
        {
            switch (path)
            {
                case WeaponUpgradePath.PathA:
                    projectilesPerVolley = nextStep switch
                    {
                        1 => 2,
                        2 => 3,
                        _ => 4
                    };
                    spreadAngle = projectilesPerVolley switch
                    {
                        1 => 0f,
                        2 => 8f,
                        3 => 14f,
                        _ => 20f
                    };
                    break;
                case WeaponUpgradePath.PathB:
                    projectileDamage = nextStep switch
                    {
                        1 => 1.4f,
                        2 => 1.8f,
                        _ => 2.2f
                    };
                    break;
            }
        }

        protected override string GetUpgradeTitle(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "Split Cast I",
                WeaponUpgradePath.PathA when nextStep == 2 => "Split Cast II",
                WeaponUpgradePath.PathA => "Split Cast III",
                WeaponUpgradePath.PathB when nextStep == 1 => "Barbed Iron I",
                WeaponUpgradePath.PathB when nextStep == 2 => "Barbed Iron II",
                _ => "Barbed Iron III"
            };
        }

        protected override string GetUpgradeDescription(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "Increase harpoon count to 2 with a wider volley.",
                WeaponUpgradePath.PathA when nextStep == 2 => "Increase harpoon count to 3 with a broader volley.",
                WeaponUpgradePath.PathA => "Increase harpoon count to 4 and flood the lane.",
                WeaponUpgradePath.PathB when nextStep == 1 => "Increase harpoon damage to 1.4.",
                WeaponUpgradePath.PathB when nextStep == 2 => "Increase harpoon damage to 1.8.",
                _ => "Increase harpoon damage to 2.2."
            };
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
