using System.Collections;
using System.Collections.Generic;
using Dagon.Core;
using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    public sealed class BilgeSprayWeapon : PlayerWeaponRuntime
    {
        [System.Serializable]
        internal struct VisualPreset
        {
            public string spriteResourcePath;
            public Color tint;
            public float heightOffset;
            public float forwardOffsetNormalized;
            public float lengthMultiplier;
            public float widthMultiplier;
            public float nearWidthFactor;
            public float duration;
            public float endScaleMultiplier;
            public int sortingOrder;

            public static VisualPreset CreateDefault()
            {
                return new VisualPreset
                {
                    spriteResourcePath = string.Empty,
                    tint = new Color(0.40f, 0.86f, 0.48f, 0.30f),
                    heightOffset = 0.05f,
                    forwardOffsetNormalized = 0.14f,
                    lengthMultiplier = 1.05f,
                    widthMultiplier = 1.15f,
                    nearWidthFactor = 0.22f,
                    duration = 0.18f,
                    endScaleMultiplier = 1.04f,
                    sortingOrder = 4
                };
            }
        }

        [SerializeField] private PlayerMover playerMover;
        [SerializeField] private float attacksPerSecond = 0.65f;
        [SerializeField] private float damage = 0.7f;
        [SerializeField] private float range = 4.8f;
        [SerializeField] private float coneAngle = 120f;
        [SerializeField] private float slowAmount = 0.25f;
        [SerializeField] private float slowDuration = 1.5f;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private VisualPreset visualPreset;

        private Camera worldCamera;
        private float cooldownTimer;
        private readonly HashSet<GameObject> resolvedTargets = new();
        private float baseDamage;
        private float baseRange;
        public override string PathAName => "Pressure Wash";
        public override string PathBName => "Foul Brine";

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

            StartCoroutine(FireBurstSequence());
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
            damage = Mathf.Max(0.1f, damage + amount);
            baseDamage = Mathf.Max(0.1f, baseDamage + amount);
        }

        public override void ModifyProjectileCount(int amount)
        {
            // Bilge Spray stays a single instant pulse.
        }

        protected override void ApplyDefinition(WeaponDefinition runtimeDefinition)
        {
            attacksPerSecond = Mathf.Max(0.1f, runtimeDefinition.AttacksPerSecond);
            damage = Mathf.Max(0.1f, runtimeDefinition.ProjectileDamage);
            range = Mathf.Max(0.5f, runtimeDefinition.EffectRadius);
            coneAngle = Mathf.Clamp(runtimeDefinition.EffectAngle, 35f, 140f);
            slowAmount = Mathf.Clamp01(runtimeDefinition.SlowAmount);
            slowDuration = Mathf.Max(0f, runtimeDefinition.SlowDuration);
            baseDamage = damage;
            baseRange = range;
        }

        protected override void ApplyPathUpgrade(WeaponUpgradePath path, int nextStep)
        {
            switch (path)
            {
                case WeaponUpgradePath.PathA:
                    range = baseRange * (nextStep switch
                    {
                        1 => 1.15f,
                        2 => 1.3f,
                        _ => 1.45f
                    });
                    break;
                case WeaponUpgradePath.PathB:
                    damage = baseDamage * (nextStep switch
                    {
                        1 => 1.4f,
                        2 => 2f,
                        _ => 2.6f
                    });
                    break;
            }
        }

        protected override string GetUpgradeTitle(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "Bilge Pump I",
                WeaponUpgradePath.PathA when nextStep == 2 => "Bilge Pump II",
                WeaponUpgradePath.PathA => "Bilge Pump III",
                WeaponUpgradePath.PathB when nextStep == 1 => "Foul Brine I",
                WeaponUpgradePath.PathB when nextStep == 2 => "Foul Brine II",
                _ => "Foul Brine III"
            };
        }

        protected override string GetUpgradeDescription(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "Increase spray area by 15%.",
                WeaponUpgradePath.PathA when nextStep == 2 => "Increase spray area by 30%.",
                WeaponUpgradePath.PathA => "Increase spray area by 45%.",
                WeaponUpgradePath.PathB when nextStep == 1 => "Increase spray damage by 40%.",
                WeaponUpgradePath.PathB when nextStep == 2 => "Double spray damage.",
                _ => "Increase spray damage by 160%."
            };
        }

        private IEnumerator FireBurstSequence()
        {
            Spray(ResolveAimDirection());
            yield break;
        }

        private void Spray(Vector3 aim)
        {
            var origin = transform.position;
            var halfAngle = coneAngle * 0.5f;
            var colliders = Physics.OverlapSphere(origin, range, enemyMask, QueryTriggerInteraction.Collide);

            resolvedTargets.Clear();
            foreach (var hit in colliders)
            {
                if (hit.transform == transform)
                {
                    continue;
                }

                var targetPoint = hit.bounds.center;
                var toTarget = targetPoint - origin;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude <= 0.01f)
                {
                    continue;
                }

                if (toTarget.sqrMagnitude > range * range)
                {
                    continue;
                }

                if (Vector3.Angle(aim, toTarget.normalized) > halfAngle)
                {
                    continue;
                }

                if (!CombatResolver.TryResolveUniqueHit(hit, CombatTeam.Player, gameObject, resolvedTargets, out var resolvedHit))
                {
                    continue;
                }

                CombatResolver.TryApplyDamage(resolvedHit, gameObject, damage, CombatTeam.Player);

                var slowReceiver = resolvedHit.TargetRoot != null ? resolvedHit.TargetRoot.GetComponent<EnemySlowReceiver>() : null;
                if (slowReceiver == null && resolvedHit.Hurtbox != null && resolvedHit.Hurtbox.Health != null)
                {
                    slowReceiver = resolvedHit.Hurtbox.Health.GetComponent<EnemySlowReceiver>() ??
                                   resolvedHit.Hurtbox.Health.gameObject.AddComponent<EnemySlowReceiver>();
                }

                slowReceiver?.ApplySlow(slowAmount, slowDuration);
            }

            BilgeSprayWedgeVisual.Spawn(origin, aim, range, coneAngle, worldCamera, ResolveVisualPreset());
        }

        private Vector3 ResolveAimDirection()
        {
            var aim = playerMover != null ? playerMover.AimDirection : transform.forward;
            return aim.sqrMagnitude > 0.001f ? aim.normalized : transform.forward;
        }

        private void EnsureVisualPreset()
        {
            if (visualPreset.duration > 0f ||
                visualPreset.endScaleMultiplier > 0f ||
                visualPreset.widthMultiplier > 0f ||
                visualPreset.lengthMultiplier > 0f ||
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
            public readonly float ForwardOffsetNormalized;
            public readonly float LengthMultiplier;
            public readonly float WidthMultiplier;
            public readonly float NearWidthFactor;
            public readonly float Duration;
            public readonly float EndScaleMultiplier;
            public readonly int SortingOrder;

            public VisualResolved(VisualPreset preset)
            {
                SpriteResourcePath = preset.spriteResourcePath;
                Tint = preset.tint;
                HeightOffset = preset.heightOffset;
                ForwardOffsetNormalized = preset.forwardOffsetNormalized;
                LengthMultiplier = preset.lengthMultiplier;
                WidthMultiplier = preset.widthMultiplier;
                NearWidthFactor = preset.nearWidthFactor;
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
