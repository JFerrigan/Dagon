using System.Collections;
using System.Collections.Generic;
using Dagon.Core;
using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    public sealed class AnchorChainWeapon : PlayerWeaponRuntime
    {
        [System.Serializable]
        internal struct VisualPreset
        {
            public string spriteResourcePath;
            public Color tint;
            public float heightOffset;
            public float outerRadiusMultiplier;
            public float innerRadiusFactor;
            public float duration;
            public float endScaleMultiplier;
            public int sortingOrder;

            public static VisualPreset CreateDefault()
            {
                return new VisualPreset
                {
                    spriteResourcePath = string.Empty,
                    tint = new Color(0.70f, 0.84f, 0.76f, 0.26f),
                    heightOffset = 0.05f,
                    outerRadiusMultiplier = 1.05f,
                    innerRadiusFactor = 0.28f,
                    duration = 0.22f,
                    endScaleMultiplier = 1.04f,
                    sortingOrder = 4
                };
            }
        }

        [SerializeField] private PlayerMover playerMover;
        [SerializeField] private float attacksPerSecond = 0.85f;
        [SerializeField] private float damage = 1.8f;
        [SerializeField] private float radius = 2.4f;
        [SerializeField] private float arcAngle = 105f;
        [SerializeField] private float knockbackForce = 4.5f;
        [SerializeField] private float eliteKnockbackMultiplier = 0.35f;
        [SerializeField] private int arcCount = 1;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private VisualPreset visualPreset;

        private Camera worldCamera;
        private float cooldownTimer;
        private readonly HashSet<GameObject> resolvedTargets = new();

        public override string PathAName => "Chain Flurry";
        public override string PathBName => "Heavy Anchor";

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
        }

        public override void ModifyAttackRate(float amount)
        {
            attacksPerSecond = Mathf.Max(0.1f, attacksPerSecond + amount);
        }

        public override void ModifyProjectileDamage(float amount)
        {
            damage = Mathf.Max(0.1f, damage + amount);
        }

        public override void ModifyProjectileCount(int amount)
        {
            arcCount = Mathf.Max(1, arcCount + amount);
        }

        public override float GetAttackRateEstimate()
        {
            return attacksPerSecond;
        }

        protected override void ApplyDefinition(WeaponDefinition runtimeDefinition)
        {
            attacksPerSecond = Mathf.Max(0.1f, runtimeDefinition.AttacksPerSecond);
            damage = Mathf.Max(0.1f, runtimeDefinition.ProjectileDamage);
            radius = Mathf.Max(0.5f, runtimeDefinition.EffectRadius);
            arcAngle = Mathf.Max(10f, runtimeDefinition.EffectAngle);
            knockbackForce = Mathf.Max(0f, runtimeDefinition.KnockbackForce);
            arcCount = Mathf.Max(1, runtimeDefinition.ProjectilesPerVolley);
        }

        protected override void ApplyPathUpgrade(WeaponUpgradePath path, int nextStep)
        {
            switch (path)
            {
                case WeaponUpgradePath.PathA:
                    if (nextStep <= 2)
                    {
                        arcCount = nextStep + 1;
                    }
                    else
                    {
                        damage = 2.2f;
                    }
                    break;
                case WeaponUpgradePath.PathB:
                    damage = nextStep switch
                    {
                        1 => 2.5f,
                        2 => 3.2f,
                        _ => 4.0f
                    };
                    break;
            }
        }

        protected override string GetUpgradeTitle(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "Deck Sweep I",
                WeaponUpgradePath.PathA when nextStep == 2 => "Deck Sweep II",
                WeaponUpgradePath.PathA => "Deck Sweep III",
                WeaponUpgradePath.PathB when nextStep == 1 => "Heavy Anchor I",
                WeaponUpgradePath.PathB when nextStep == 2 => "Heavy Anchor II",
                _ => "Heavy Anchor III"
            };
        }

        protected override string GetUpgradeDescription(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => FlatCountDelta(1, "Sweep"),
                WeaponUpgradePath.PathA when nextStep == 2 => FlatCountDelta(1, "Sweep"),
                WeaponUpgradePath.PathA => "+4 Sweep DMG",
                WeaponUpgradePath.PathB when nextStep == 1 => FlatDamageDelta(0.7f),
                WeaponUpgradePath.PathB when nextStep == 2 => FlatDamageDelta(0.7f),
                _ => FlatDamageDelta(0.8f)
            };
        }

        private IEnumerator FireSequence()
        {
            var baseDirection = ResolveAimDirection();
            for (var i = 0; i < arcCount; i++)
            {
                PerformSweep(baseDirection, i);
                if (arcCount > 1 && i < arcCount - 1)
                {
                    yield return new WaitForSeconds(0.08f);
                }
            }
        }

        private void PerformSweep(Vector3 baseDirection, int sweepIndex)
        {
            var colliders = Physics.OverlapSphere(transform.position, radius, enemyMask, QueryTriggerInteraction.Collide);
            resolvedTargets.Clear();
            foreach (var hit in colliders)
            {
                if (hit.transform == transform)
                {
                    continue;
                }

                var toTarget = hit.transform.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude <= 0.01f)
                {
                    continue;
                }

                if (Vector3.Angle(baseDirection, toTarget.normalized) > arcAngle * 0.5f)
                {
                    continue;
                }

                if (!CombatResolver.TryResolveUniqueHit(hit, CombatTeam.Player, gameObject, resolvedTargets, out var resolvedHit))
                {
                    continue;
                }

                if (CombatResolver.TryApplyDamage(resolvedHit, gameObject, damage, CombatTeam.Player))
                {
                    ApplyKnockback(resolvedHit.TargetRoot != null ? resolvedHit.TargetRoot.transform : hit.transform, toTarget.normalized);
                }
            }

            AnchorChainArcVisual.Spawn(
                transform.position,
                baseDirection,
                radius,
                arcAngle,
                sweepIndex * 4f,
                worldCamera,
                ResolveVisualPreset());
        }

        private Vector3 ResolveAimDirection()
        {
            var aim = playerMover != null ? playerMover.AimDirection : transform.forward;
            return aim.sqrMagnitude > 0.001f ? aim.normalized : transform.forward;
        }

        private void ApplyKnockback(Transform target, Vector3 direction)
        {
            if (target == null || IsHeavyEnemy(target))
            {
                return;
            }

            target.position += direction * (knockbackForce * Time.deltaTime);
        }

        private bool IsHeavyEnemy(Transform target)
        {
            return target.GetComponentInParent<DeepSpawnBruiser>() != null ||
                   target.GetComponentInParent<MireColossusController>() != null;
        }

        private void EnsureVisualPreset()
        {
            if (visualPreset.duration > 0f ||
                visualPreset.endScaleMultiplier > 0f ||
                visualPreset.outerRadiusMultiplier > 0f ||
                visualPreset.sortingOrder != 0 ||
                visualPreset.tint.a > 0f)
            {
                return;
            }

            visualPreset = VisualPreset.CreateDefault();
        }

        internal readonly struct VisualResolved
        {
            public readonly string SpriteResourcePath;
            public readonly Color Tint;
            public readonly float HeightOffset;
            public readonly float OuterRadiusMultiplier;
            public readonly float InnerRadiusFactor;
            public readonly float Duration;
            public readonly float EndScaleMultiplier;
            public readonly int SortingOrder;

            public VisualResolved(VisualPreset preset)
            {
                SpriteResourcePath = preset.spriteResourcePath;
                Tint = preset.tint;
                HeightOffset = preset.heightOffset;
                OuterRadiusMultiplier = preset.outerRadiusMultiplier;
                InnerRadiusFactor = preset.innerRadiusFactor;
                Duration = preset.duration;
                EndScaleMultiplier = preset.endScaleMultiplier;
                SortingOrder = preset.sortingOrder;
            }
        }

        internal VisualResolved ResolveVisualPreset()
        {
            EnsureVisualPreset();
            return new VisualResolved(visualPreset);
        }
    }
}
