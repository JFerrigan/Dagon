using System.Collections;
using Dagon.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dagon.Gameplay
{
    public sealed class RotBeaconBombWeapon : PlayerWeaponRuntime
    {
        [System.Serializable]
        private struct VisualPreset
        {
            public string projectileSpritePath;
            public Color projectileTint;
            public Vector3 projectileScale;
            public string beaconSpritePath;
            public Color beaconTint;
            public Vector3 beaconScale;
            public string pulseSpritePath;
            public Color pulseTint;
            public string explosionSpritePath;
            public Color explosionTint;
            public float visualHeightOffset;
            public int projectileSortingOrder;
            public int beaconSortingOrder;
            public int effectSortingOrder;

            public static VisualPreset CreateDefault()
            {
                return new VisualPreset
                {
                    projectileSpritePath = "Sprites/Weapons/rot_lantern",
                    projectileTint = new Color(0.74f, 0.98f, 0.74f, 0.95f),
                    projectileScale = new Vector3(0.38f, 0.38f, 1f),
                    beaconSpritePath = "Sprites/Weapons/rot_lantern",
                    beaconTint = new Color(0.80f, 1f, 0.78f, 0.92f),
                    beaconScale = new Vector3(0.52f, 0.52f, 1f),
                    pulseSpritePath = "Sprites/Effects/brine_surge",
                    pulseTint = new Color(0.50f, 0.92f, 0.58f, 0.34f),
                    explosionSpritePath = "Sprites/Effects/brine_surge",
                    explosionTint = new Color(0.86f, 1f, 0.82f, 0.46f),
                    visualHeightOffset = 0.08f,
                    projectileSortingOrder = 10,
                    beaconSortingOrder = 9,
                    effectSortingOrder = 4
                };
            }
        }

        [SerializeField] private PlayerMover playerMover;
        [SerializeField] private float attacksPerSecond = 0.55f;
        [SerializeField] private float pulseDamage = 0.45f;
        [SerializeField] private float throwRange = 8f;
        [SerializeField] private int pulseCount = 2;
        [SerializeField] private float pulseRadius = 2.6f;
        [SerializeField] private float explosionRadius = 3.6f;
        [SerializeField] private float explosionDamage = 1.8f;
        [SerializeField] private float slowAmount = 0.25f;
        [SerializeField] private float slowDuration = 1.5f;
        [SerializeField] private float travelDuration = 0.38f;
        [SerializeField] private float arcHeight = 1.35f;
        [SerializeField] private float pulseInterval = 0.55f;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private VisualPreset visualPreset;

        private Camera worldCamera;
        private float cooldownTimer;
        private float basePulseRadius;
        private int basePulseCount;
        private float baseExplosionRadius;
        private float baseExplosionDamage;

        public override string PathAName => "Beacon Choir";
        public override string PathBName => "Rupture Charge";

        private void Awake()
        {
            if (playerMover == null)
            {
                playerMover = GetComponent<PlayerMover>();
            }

            EnsureVisualPreset();
        }

        private void OnValidate()
        {
            EnsureVisualPreset();
        }

        private void Update()
        {
            if (attacksPerSecond <= 0f)
            {
                return;
            }

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer > 0f)
            {
                return;
            }

            StartCoroutine(FireSequence());
            cooldownTimer = 1f / attacksPerSecond;
        }

        public override void ConfigureRuntime(Camera worldCameraReference)
        {
            worldCamera = worldCameraReference;
            EnsureVisualPreset();
        }

        public override void ModifyAttackRate(float amount)
        {
            attacksPerSecond = Mathf.Max(0.1f, attacksPerSecond + amount);
        }

        public override void ModifyProjectileDamage(float amount)
        {
            pulseDamage = Mathf.Max(0.1f, pulseDamage + amount);
            explosionDamage = Mathf.Max(0.2f, explosionDamage + amount);
            baseExplosionDamage = Mathf.Max(0.2f, baseExplosionDamage + amount);
        }

        public override void ModifyProjectileCount(int amount)
        {
            pulseCount = Mathf.Max(1, pulseCount + amount);
            basePulseCount = Mathf.Max(1, basePulseCount + amount);
        }

        protected override void ApplyDefinition(WeaponDefinition runtimeDefinition)
        {
            attacksPerSecond = Mathf.Max(0.1f, runtimeDefinition.AttacksPerSecond);
            throwRange = Mathf.Max(2f, runtimeDefinition.ProjectileSpeed);
            pulseDamage = Mathf.Max(0.1f, runtimeDefinition.ProjectileDamage);
            pulseCount = Mathf.Max(1, runtimeDefinition.ProjectilesPerVolley);
            pulseRadius = Mathf.Max(0.5f, runtimeDefinition.EffectRadius);
            explosionRadius = Mathf.Max(0.75f, runtimeDefinition.EffectAngle);
            explosionDamage = Mathf.Max(0.2f, runtimeDefinition.KnockbackForce);
            slowAmount = Mathf.Clamp01(runtimeDefinition.SlowAmount);
            slowDuration = Mathf.Max(0f, runtimeDefinition.SlowDuration);
            basePulseRadius = pulseRadius;
            basePulseCount = pulseCount;
            baseExplosionRadius = explosionRadius;
            baseExplosionDamage = explosionDamage;
        }

        protected override void ApplyPathUpgrade(WeaponUpgradePath path, int nextStep)
        {
            switch (path)
            {
                case WeaponUpgradePath.PathA:
                    switch (nextStep)
                    {
                        case 1:
                            pulseCount = basePulseCount + 1;
                            break;
                        case 2:
                            pulseRadius = basePulseRadius * 1.22f;
                            break;
                        default:
                            slowAmount = Mathf.Clamp01(slowAmount + 0.18f);
                            slowDuration += 0.5f;
                            break;
                    }
                    break;
                case WeaponUpgradePath.PathB:
                    switch (nextStep)
                    {
                        case 1:
                            explosionDamage = baseExplosionDamage * 1.45f;
                            break;
                        case 2:
                            explosionRadius = baseExplosionRadius * 1.22f;
                            break;
                        default:
                            explosionDamage = baseExplosionDamage * 2f;
                            break;
                    }
                    break;
            }
        }

        protected override string GetUpgradeTitle(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "Beacon Choir I",
                WeaponUpgradePath.PathA when nextStep == 2 => "Beacon Choir II",
                WeaponUpgradePath.PathA => "Beacon Choir III",
                WeaponUpgradePath.PathB when nextStep == 1 => "Rupture Charge I",
                WeaponUpgradePath.PathB when nextStep == 2 => "Rupture Charge II",
                _ => "Rupture Charge III"
            };
        }

        protected override string GetUpgradeDescription(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "+1 Pulse",
                WeaponUpgradePath.PathA when nextStep == 2 => "+22% Pulse Radius",
                WeaponUpgradePath.PathA => "+18% Slow, +0.5s Slow Time",
                WeaponUpgradePath.PathB when nextStep == 1 => "+45% Final Blast DMG",
                WeaponUpgradePath.PathB when nextStep == 2 => "+22% Final Blast Radius",
                _ => "Double Final Blast DMG"
            };
        }

        private IEnumerator FireSequence()
        {
            ThrowBeacon();
            yield break;
        }

        private void ThrowBeacon()
        {
            var aim = ResolveAimDirection();
            var targetPoint = ResolveTargetPoint(aim);
            var visual = ResolveVisualPreset();
            RotBeaconBombProjectile.Spawn(
                transform.position,
                targetPoint,
                travelDuration,
                arcHeight,
                worldCamera,
                gameObject,
                visual.projectileSpritePath,
                visual.projectileTint,
                visual.projectileScale,
                visual.projectileSortingOrder,
                visual.visualHeightOffset,
                pulseRadius,
                pulseCount,
                pulseInterval,
                pulseDamage,
                slowAmount,
                slowDuration,
                explosionRadius,
                explosionDamage,
                enemyMask,
                visual.beaconSpritePath,
                visual.beaconTint,
                visual.beaconScale,
                visual.beaconSortingOrder,
                visual.pulseSpritePath,
                visual.pulseTint,
                visual.explosionSpritePath,
                visual.explosionTint,
                visual.effectSortingOrder);
        }

        private Vector3 ResolveAimDirection()
        {
            var aim = playerMover != null ? playerMover.AimDirection : transform.forward;
            return aim.sqrMagnitude > 0.001f ? aim.normalized : transform.forward;
        }

        private Vector3 ResolveTargetPoint(Vector3 aim)
        {
            if (worldCamera != null && Mouse.current != null)
            {
                var ray = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                var plane = new Plane(Vector3.up, transform.position);
                if (plane.Raycast(ray, out var distance))
                {
                    var point = ray.GetPoint(distance);
                    var planar = point - transform.position;
                    planar.y = 0f;
                    if (planar.sqrMagnitude > 0.01f)
                    {
                        return transform.position + Vector3.ClampMagnitude(planar, throwRange);
                    }
                }
            }

            return transform.position + (aim * throwRange);
        }

        private void EnsureVisualPreset()
        {
            if (visualPreset.projectileScale != Vector3.zero ||
                visualPreset.beaconScale != Vector3.zero ||
                visualPreset.projectileSortingOrder != 0 ||
                visualPreset.beaconSortingOrder != 0 ||
                visualPreset.effectSortingOrder != 0 ||
                visualPreset.projectileTint.a > 0f ||
                visualPreset.beaconTint.a > 0f ||
                visualPreset.pulseTint.a > 0f ||
                visualPreset.explosionTint.a > 0f)
            {
                return;
            }

            visualPreset = VisualPreset.CreateDefault();
        }

        private VisualPreset ResolveVisualPreset()
        {
            EnsureVisualPreset();
            return visualPreset;
        }
    }
}
