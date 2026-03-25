using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    public sealed class TideburstWeapon : PlayerWeaponRuntime
    {
        private static readonly Color TideburstProjectileTint = new(0.32f, 0.68f, 0.98f, 1f);
        private static readonly Color TideburstOverlayTint = new(0.70f, 0.90f, 1f, 0.42f);

        [SerializeField] private HarpoonProjectile projectilePrefab;
        [SerializeField] private float attacksPerSecond = 0.7f;
        [SerializeField] private float projectileSpeed = 5f;
        [SerializeField] private float projectileDamage = 0.45f;
        [SerializeField] private int projectileCount = 8;
        [SerializeField] private float spawnHeight = 0.5f;
        [SerializeField] private bool hitKnockbackEnabled = true;
        [SerializeField] private float hitKnockbackStrength = 0.35f;

        private float cooldownTimer;
        private int baseProjectileCount;
        private float baseAttacksPerSecond;
        private float baseProjectileDamage;

        public override string PathAName => "Swell Path";
        public override string PathBName => "Tempest Path";

        private void Update()
        {
            if (projectilePrefab == null || attacksPerSecond <= 0f)
            {
                return;
            }

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer > 0f)
            {
                return;
            }

            FireBurst();
            cooldownTimer = 1f / attacksPerSecond;
        }

        public override void ConfigureRuntime(Camera worldCameraReference)
        {
            projectilePrefab = CreateTideburstProjectile(worldCameraReference);
        }

        public override void ModifyAttackRate(float amount)
        {
            attacksPerSecond = Mathf.Max(0.1f, attacksPerSecond + amount);
        }

        public override void ModifyProjectileDamage(float amount)
        {
            projectileDamage = Mathf.Max(0.1f, projectileDamage + amount);
            baseProjectileDamage = Mathf.Max(0.1f, baseProjectileDamage + amount);
        }

        public override void ModifyProjectileCount(int amount)
        {
            projectileCount = Mathf.Max(4, projectileCount + amount);
            baseProjectileCount = Mathf.Max(4, baseProjectileCount + amount);
        }

        public override float GetAttackRateEstimate()
        {
            return attacksPerSecond;
        }

        protected override void ApplyDefinition(WeaponDefinition runtimeDefinition)
        {
            attacksPerSecond = Mathf.Max(0.1f, runtimeDefinition.AttacksPerSecond);
            projectileSpeed = Mathf.Max(1f, runtimeDefinition.ProjectileSpeed);
            projectileDamage = Mathf.Max(0.1f, runtimeDefinition.ProjectileDamage);
            projectileCount = Mathf.Max(4, runtimeDefinition.ProjectilesPerVolley);
            hitKnockbackStrength = Mathf.Max(0f, runtimeDefinition.KnockbackForce);
            baseProjectileCount = projectileCount;
            baseAttacksPerSecond = attacksPerSecond;
            baseProjectileDamage = projectileDamage;
        }

        protected override void ApplyPathUpgrade(WeaponUpgradePath path, int nextStep)
        {
            switch (path)
            {
                case WeaponUpgradePath.PathA:
                    projectileCount = nextStep switch
                    {
                        1 => baseProjectileCount + 2,
                        2 => baseProjectileCount + 4,
                        _ => baseProjectileCount + 6
                    };
                    break;
                case WeaponUpgradePath.PathB:
                    switch (nextStep)
                    {
                        case 1:
                            attacksPerSecond = baseAttacksPerSecond * 1.35f;
                            break;
                        case 2:
                            projectileDamage = baseProjectileDamage * 1.45f;
                            break;
                        default:
                            attacksPerSecond = baseAttacksPerSecond * 1.55f;
                            projectileDamage = baseProjectileDamage * 1.7f;
                            break;
                    }
                    break;
            }
        }

        protected override void ApplyOverflowUpgrade(WeaponUpgradePath path, int nextStep)
        {
            if (path == WeaponUpgradePath.PathA)
            {
                projectileCount += 1;
                return;
            }

            attacksPerSecond *= 1.12f;
            projectileDamage *= 1.15f;
        }

        protected override string GetUpgradeTitle(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "Swell I",
                WeaponUpgradePath.PathA when nextStep == 2 => "Swell II",
                WeaponUpgradePath.PathA => "Swell III",
                WeaponUpgradePath.PathB when nextStep == 1 => "Tempest I",
                WeaponUpgradePath.PathB when nextStep == 2 => "Tempest II",
                _ => "Tempest III"
            };
        }

        protected override string GetUpgradeDescription(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "+2 Burst Projectiles",
                WeaponUpgradePath.PathA when nextStep == 2 => "+4 Burst Projectiles",
                WeaponUpgradePath.PathA => "+6 Burst Projectiles",
                WeaponUpgradePath.PathB when nextStep == 1 => "+35% Rate",
                WeaponUpgradePath.PathB when nextStep == 2 => "+45% DMG",
                _ => "+55% Rate and +70% DMG"
            };
        }

        protected override string GetOverflowUpgradeDescription(WeaponUpgradePath path, int nextStep)
        {
            return path == WeaponUpgradePath.PathA
                ? "+1 Burst Projectile"
                : "+12% Rate and +15% DMG";
        }

        private void FireBurst()
        {
            var origin = GetProjectileLaunchOrigin(spawnHeight);
            var count = Mathf.Max(4, projectileCount);
            var angleStep = 360f / count;

            for (var i = 0; i < count; i++)
            {
                var angle = angleStep * i;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var projectile = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction, Vector3.up));
                projectile.gameObject.SetActive(true);
                projectile.Initialize(gameObject, direction, projectileSpeed, projectileDamage, hitKnockbackEnabled, hitKnockbackStrength);
            }
        }

        private static HarpoonProjectile CreateTideburstProjectile(Camera worldCameraReference)
        {
            var projectile = RuntimeOrbProjectileFactory.Create(
                worldCameraReference,
                "Sprites/Enemies/mire_wretch",
                TideburstProjectileTint,
                new Vector3(0.88f, 0.88f, 1f),
                256f);
            if (projectile == null)
            {
                return null;
            }

            var visuals = projectile.transform.Find("Visuals");
            if (visuals == null)
            {
                return projectile;
            }

            var baseRenderer = visuals.GetComponent<SpriteRenderer>();
            if (baseRenderer == null || baseRenderer.sprite == null)
            {
                return projectile;
            }

            var overlay = new GameObject("TideburstOverlay");
            overlay.transform.SetParent(visuals, false);
            overlay.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            overlay.transform.localScale = Vector3.one * 1.12f;

            var overlayRenderer = overlay.AddComponent<SpriteRenderer>();
            overlayRenderer.sprite = baseRenderer.sprite;
            overlayRenderer.color = TideburstOverlayTint;
            overlayRenderer.sortingOrder = baseRenderer.sortingOrder + 1;

            return projectile;
        }
    }
}
