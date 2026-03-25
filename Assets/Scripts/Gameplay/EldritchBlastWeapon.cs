using System.Collections.Generic;
using Dagon.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dagon.Gameplay
{
    public sealed class EldritchBlastWeapon : PlayerWeaponRuntime
    {
        [System.Serializable]
        internal struct VisualPreset
        {
            public string spriteResourcePath;
            public Color coreTint;
            public Color glowTint;
            public float heightOffset;
            public float duration;
            public float endScaleMultiplier;
            public float widthMultiplier;
            public int sortingOrder;

            public static VisualPreset CreateDefault()
            {
                return new VisualPreset
                {
                    spriteResourcePath = string.Empty,
                    coreTint = new Color(0.76f, 0.20f, 0.18f, 0.70f),
                    glowTint = new Color(0.18f, 0.90f, 0.66f, 0.26f),
                    heightOffset = 0.08f,
                    duration = 0.18f,
                    endScaleMultiplier = 1.05f,
                    widthMultiplier = 1.2f,
                    sortingOrder = 18
                };
            }
        }

        [SerializeField] private PlayerMover playerMover;
        [SerializeField] private float attacksPerSecond = 0.1f;
        [SerializeField] private float beamLength = 12f;
        [SerializeField] private float beamDamage = 7f;
        [SerializeField] private float beamHalfWidth = 0.7f;
        [SerializeField] private float hitboxHeight = 2.4f;
        [SerializeField] private float originForwardOffset = 0.45f;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private VisualPreset visualPreset;

        private readonly HashSet<GameObject> resolvedTargets = new();
        private Camera worldCamera;
        private float cooldownTimer;
        private float baseBeamLength;
        private float baseBeamDamage;
        private float baseBeamHalfWidth;

        public override string PathAName => "Widening Rift";
        public override string PathBName => "Void Pressure";

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
            beamDamage = Mathf.Max(0.1f, beamDamage + amount);
            baseBeamDamage = Mathf.Max(0.1f, baseBeamDamage + amount);
        }

        public override void ModifyProjectileCount(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            beamHalfWidth = Mathf.Max(0.25f, beamHalfWidth + (amount * 0.12f));
            baseBeamHalfWidth = Mathf.Max(0.25f, baseBeamHalfWidth + (amount * 0.12f));
        }

        public override float GetAttackRateEstimate()
        {
            return attacksPerSecond;
        }

        protected override void ApplyDefinition(WeaponDefinition runtimeDefinition)
        {
            attacksPerSecond = Mathf.Max(0.1f, runtimeDefinition.AttacksPerSecond);
            beamLength = Mathf.Max(2f, runtimeDefinition.EffectAngle);
            beamDamage = Mathf.Max(0.1f, runtimeDefinition.ProjectileDamage);
            beamHalfWidth = Mathf.Max(0.2f, runtimeDefinition.EffectRadius);
            baseBeamLength = beamLength;
            baseBeamDamage = beamDamage;
            baseBeamHalfWidth = beamHalfWidth;
        }

        protected override void ApplyPathUpgrade(WeaponUpgradePath path, int nextStep)
        {
            switch (path)
            {
                case WeaponUpgradePath.PathA:
                    switch (nextStep)
                    {
                        case 1:
                            beamLength = baseBeamLength * 1.25f;
                            break;
                        case 2:
                            beamHalfWidth = baseBeamHalfWidth * 1.45f;
                            break;
                        default:
                            beamLength = baseBeamLength * 1.45f;
                            beamHalfWidth = baseBeamHalfWidth * 1.7f;
                            break;
                    }
                    break;
                case WeaponUpgradePath.PathB:
                    switch (nextStep)
                    {
                        case 1:
                            beamDamage = baseBeamDamage * 1.45f;
                            break;
                        case 2:
                            beamDamage = baseBeamDamage * 1.9f;
                            break;
                        default:
                            beamDamage = baseBeamDamage * 2.35f;
                            attacksPerSecond = Mathf.Max(attacksPerSecond, definition != null ? definition.AttacksPerSecond * 1.15f : attacksPerSecond);
                            break;
                    }
                    break;
            }
        }

        protected override void ApplyOverflowUpgrade(WeaponUpgradePath path, int nextStep)
        {
            if (path == WeaponUpgradePath.PathA)
            {
                beamLength *= 1.08f;
                beamHalfWidth *= 1.08f;
                return;
            }

            beamDamage *= 1.14f;
            attacksPerSecond *= 1.03f;
        }

        protected override string GetUpgradeTitle(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "Widening Rift I",
                WeaponUpgradePath.PathA when nextStep == 2 => "Widening Rift II",
                WeaponUpgradePath.PathA => "Widening Rift III",
                WeaponUpgradePath.PathB when nextStep == 1 => "Void Pressure I",
                WeaponUpgradePath.PathB when nextStep == 2 => "Void Pressure II",
                _ => "Void Pressure III"
            };
        }

        protected override string GetUpgradeDescription(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "+25% Beam Length",
                WeaponUpgradePath.PathA when nextStep == 2 => "+45% Beam Width",
                WeaponUpgradePath.PathA => "+45% Length and +70% Width",
                WeaponUpgradePath.PathB when nextStep == 1 => "+45% Beam DMG",
                WeaponUpgradePath.PathB when nextStep == 2 => "+90% Beam DMG",
                _ => "+135% Beam DMG and +15% Rate"
            };
        }

        protected override string GetOverflowUpgradeDescription(WeaponUpgradePath path, int nextStep)
        {
            return path == WeaponUpgradePath.PathA
                ? "+8% Beam Length and Width"
                : "+14% Beam DMG and +3% Rate";
        }

        private void Fire()
        {
            var direction = ResolveTargetDirection();
            var normalizedDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
            var center = transform.position + (normalizedDirection * (Mathf.Max(0f, originForwardOffset) + (beamLength * 0.5f)));
            var yaw = Mathf.Atan2(-normalizedDirection.z, normalizedDirection.x) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            var halfExtents = new Vector3(beamLength * 0.5f, hitboxHeight * 0.5f, beamHalfWidth);

            resolvedTargets.Clear();
            var colliders = Physics.OverlapBox(center, halfExtents, rotation, enemyMask, QueryTriggerInteraction.Collide);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (!CombatResolver.TryResolveUniqueHit(colliders[i], CombatTeam.Player, gameObject, resolvedTargets, out var resolvedHit))
                {
                    continue;
                }

                CombatResolver.TryApplyDamage(resolvedHit, gameObject, beamDamage, CombatTeam.Player);
            }

            EldritchBlastBeamVisual.Spawn(transform.position, normalizedDirection, beamLength, beamHalfWidth, ResolveVisualPreset());
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
            if (visualPreset.coreTint.a > 0f ||
                visualPreset.glowTint.a > 0f ||
                visualPreset.heightOffset != 0f ||
                visualPreset.duration != 0f ||
                visualPreset.endScaleMultiplier != 0f ||
                visualPreset.widthMultiplier != 0f ||
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
                CoreTint = preset.coreTint;
                GlowTint = preset.glowTint;
                HeightOffset = preset.heightOffset;
                Duration = preset.duration;
                EndScaleMultiplier = preset.endScaleMultiplier;
                WidthMultiplier = preset.widthMultiplier;
                SortingOrder = preset.sortingOrder;
            }

            public string SpriteResourcePath { get; }
            public Color CoreTint { get; }
            public Color GlowTint { get; }
            public float HeightOffset { get; }
            public float Duration { get; }
            public float EndScaleMultiplier { get; }
            public float WidthMultiplier { get; }
            public int SortingOrder { get; }
        }
    }
}
