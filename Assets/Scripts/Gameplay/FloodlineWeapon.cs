using Dagon.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dagon.Gameplay
{
    public sealed class FloodlineWeapon : PlayerWeaponRuntime
    {
        [System.Serializable]
        internal struct VisualPreset
        {
            public string spriteResourcePath;
            public Color tint;
            public float heightOffset;
            public float widthMultiplier;
            public float lengthMultiplier;
            public float endScaleMultiplier;
            public int sortingOrder;

            public static VisualPreset CreateDefault()
            {
                return new VisualPreset
                {
                    spriteResourcePath = string.Empty,
                    tint = new Color(0.24f, 0.86f, 0.66f, 0.42f),
                    heightOffset = 0.06f,
                    widthMultiplier = 1.05f,
                    lengthMultiplier = 1f,
                    endScaleMultiplier = 1.03f,
                    sortingOrder = 4
                };
            }
        }

        [SerializeField] private PlayerMover playerMover;
        [SerializeField] private float attacksPerSecond = 0.6f;
        [SerializeField] private float travelSpeed = 4.6f;
        [SerializeField] private float waveDamage = 0.75f;
        [SerializeField] private float halfWidth = 1.15f;
        [SerializeField] private float travelDistance = 6f;
        [SerializeField] private float knockbackForce = 14f;
        [SerializeField] private float waveLength = 2.8f;
        [SerializeField] private float hitboxHeight = 2.2f;
        [SerializeField] private float spawnForwardOffset = 0.35f;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private VisualPreset visualPreset;

        private Camera worldCamera;
        private float cooldownTimer;
        private float baseHalfWidth;
        private float baseTravelDistance;
        private float baseWaveDamage;
        private float baseKnockbackForce;

        public override string PathAName => "Undertow";
        public override string PathBName => "Breaker";

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

            Fire();
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
            waveDamage = Mathf.Max(0.1f, waveDamage + amount);
            baseWaveDamage = Mathf.Max(0.1f, baseWaveDamage + amount);
        }

        public override void ModifyProjectileCount(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            halfWidth = Mathf.Max(0.35f, halfWidth + (amount * 0.35f));
            baseHalfWidth = Mathf.Max(0.35f, baseHalfWidth + (amount * 0.35f));
        }

        public override float GetAttackRateEstimate()
        {
            return attacksPerSecond;
        }

        protected override void ApplyDefinition(WeaponDefinition runtimeDefinition)
        {
            attacksPerSecond = Mathf.Max(0.1f, runtimeDefinition.AttacksPerSecond);
            travelSpeed = Mathf.Max(1.5f, runtimeDefinition.ProjectileSpeed);
            waveDamage = Mathf.Max(0.1f, runtimeDefinition.ProjectileDamage);
            halfWidth = Mathf.Max(0.35f, runtimeDefinition.EffectRadius);
            travelDistance = Mathf.Max(1.5f, runtimeDefinition.EffectAngle);
            knockbackForce = Mathf.Max(0f, runtimeDefinition.KnockbackForce);
            baseHalfWidth = halfWidth;
            baseTravelDistance = travelDistance;
            baseWaveDamage = waveDamage;
            baseKnockbackForce = knockbackForce;
        }

        protected override void ApplyPathUpgrade(WeaponUpgradePath path, int nextStep)
        {
            switch (path)
            {
                case WeaponUpgradePath.PathA:
                    switch (nextStep)
                    {
                        case 1:
                            halfWidth = baseHalfWidth * 1.55f;
                            break;
                        case 2:
                            travelDistance = baseTravelDistance * 1.6f;
                            break;
                        default:
                            halfWidth = baseHalfWidth * 1.75f;
                            travelDistance = baseTravelDistance * 1.85f;
                            break;
                    }
                    break;
                case WeaponUpgradePath.PathB:
                    switch (nextStep)
                    {
                        case 1:
                            waveDamage = baseWaveDamage * 1.4f;
                            break;
                        case 2:
                            knockbackForce = baseKnockbackForce * 1.35f;
                            break;
                        default:
                            waveDamage = baseWaveDamage * 1.6f;
                            knockbackForce = baseKnockbackForce * 1.55f;
                            break;
                    }
                    break;
            }
        }

        protected override void ApplyOverflowUpgrade(WeaponUpgradePath path, int nextStep)
        {
            if (path == WeaponUpgradePath.PathA)
            {
                halfWidth *= 1.16f;
                travelDistance *= 1.16f;
                return;
            }

            waveDamage *= 1.15f;
            knockbackForce *= 1.12f;
        }

        protected override string GetUpgradeTitle(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "Undertow I",
                WeaponUpgradePath.PathA when nextStep == 2 => "Undertow II",
                WeaponUpgradePath.PathA => "Undertow III",
                WeaponUpgradePath.PathB when nextStep == 1 => "Breaker I",
                WeaponUpgradePath.PathB when nextStep == 2 => "Breaker II",
                _ => "Breaker III"
            };
        }

        protected override string GetUpgradeDescription(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "+55% Hit Width",
                WeaponUpgradePath.PathA when nextStep == 2 => "+60% Travel Distance",
                WeaponUpgradePath.PathA => "+75% Width and +85% Distance",
                WeaponUpgradePath.PathB when nextStep == 1 => "+40% Wave DMG",
                WeaponUpgradePath.PathB when nextStep == 2 => "+35% Knockback",
                _ => "+60% DMG and +55% Knockback"
            };
        }

        protected override string GetOverflowUpgradeDescription(WeaponUpgradePath path, int nextStep)
        {
            return path == WeaponUpgradePath.PathA
                ? "+16% Width and Distance"
                : "+15% DMG and +12% Knockback";
        }

        private void Fire()
        {
            var direction = ResolveTargetDirection();
            FloodlineWave.Spawn(
                transform.position,
                direction,
                travelSpeed,
                travelDistance,
                waveLength,
                halfWidth,
                hitboxHeight,
                spawnForwardOffset,
                waveDamage,
                knockbackForce,
                enemyMask,
                gameObject,
                ResolveVisualPreset());
        }

        private Vector3 ResolveTargetDirection()
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
                        return planar.normalized;
                    }
                }
            }

            var aim = playerMover != null ? playerMover.AimDirection : transform.forward;
            aim.y = 0f;
            return aim.sqrMagnitude > 0.001f ? aim.normalized : transform.forward;
        }

        private void EnsureVisualPreset()
        {
            if (visualPreset.tint.a > 0f ||
                visualPreset.heightOffset != 0f ||
                visualPreset.widthMultiplier != 0f ||
                visualPreset.lengthMultiplier != 0f ||
                visualPreset.endScaleMultiplier != 0f ||
                visualPreset.sortingOrder != 0 ||
                !string.IsNullOrWhiteSpace(visualPreset.spriteResourcePath))
            {
                return;
            }

            visualPreset = VisualPreset.CreateDefault();
        }

        internal VisualResolved ResolveVisualPreset()
        {
            EnsureVisualPreset();
            return new VisualResolved(visualPreset);
        }

        internal readonly struct VisualResolved
        {
            public VisualResolved(VisualPreset preset)
            {
                SpriteResourcePath = preset.spriteResourcePath;
                Tint = preset.tint;
                HeightOffset = preset.heightOffset;
                WidthMultiplier = preset.widthMultiplier;
                LengthMultiplier = preset.lengthMultiplier;
                EndScaleMultiplier = preset.endScaleMultiplier;
                SortingOrder = preset.sortingOrder;
            }

            public string SpriteResourcePath { get; }
            public Color Tint { get; }
            public float HeightOffset { get; }
            public float WidthMultiplier { get; }
            public float LengthMultiplier { get; }
            public float EndScaleMultiplier { get; }
            public int SortingOrder { get; }
        }
    }
}
