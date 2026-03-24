using System.Collections;
using System.Collections.Generic;
using Dagon.Core;
using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    public sealed class RotLanternWeapon : PlayerWeaponRuntime
    {
        private const string LanternSpritePath = "Sprites/Weapons/rot_lantern";

        [SerializeField] private float pulseRate = 0.75f;
        [SerializeField] private float damage = 0.8f;
        [SerializeField] private float radius = 2.2f;
        [SerializeField] private int pulseCount = 1;
        [SerializeField] private LayerMask enemyMask = ~0;
        [Header("Radius Indicator")]
        [SerializeField] private bool showRadiusIndicator = true;
        [SerializeField] private float radiusIndicatorHeightOffset = 0.05f;
        [SerializeField] private float radiusIndicatorThickness = 0.18f;
        [SerializeField] private Color radiusIndicatorTint = new(0.56f, 0.92f, 0.62f, 0.30f);
        [SerializeField] private float radiusIndicatorDuration = 0.32f;
        [SerializeField] private float radiusIndicatorEndScaleMultiplier = 1.04f;
        [SerializeField] private int radiusIndicatorSortingOrder = 4;

        private Camera worldCamera;
        private float cooldownTimer = 0.2f;
        private readonly HashSet<GameObject> resolvedTargets = new();

        public override string PathAName => "Lantern Choir";
        public override string PathBName => "Baleful Flame";

        private void Update()
        {
            if (pulseRate <= 0f)
            {
                return;
            }

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer > 0f)
            {
                return;
            }

            StartCoroutine(PulseSequence());
            cooldownTimer = 1f / pulseRate;
        }

        public override void ConfigureRuntime(Camera worldCameraReference)
        {
            worldCamera = worldCameraReference;
        }

        public override void ModifyAttackRate(float amount)
        {
            pulseRate = Mathf.Max(0.1f, pulseRate + amount);
        }

        public override void ModifyProjectileDamage(float amount)
        {
            damage = Mathf.Max(0.1f, damage + amount);
        }

        public override void ModifyProjectileCount(int amount)
        {
            pulseCount = Mathf.Max(1, pulseCount + amount);
        }

        public override float GetAttackRateEstimate()
        {
            return pulseRate;
        }

        protected override void ApplyDefinition(WeaponDefinition runtimeDefinition)
        {
            pulseRate = Mathf.Max(0.1f, runtimeDefinition.AttacksPerSecond);
            damage = Mathf.Max(0.1f, runtimeDefinition.ProjectileDamage);
            radius = Mathf.Max(0.5f, runtimeDefinition.EffectRadius);
            pulseCount = Mathf.Max(1, runtimeDefinition.ProjectilesPerVolley);
        }

        protected override void ApplyPathUpgrade(WeaponUpgradePath path, int nextStep)
        {
            switch (path)
            {
                case WeaponUpgradePath.PathA:
                    pulseCount = nextStep + 1;
                    break;
                case WeaponUpgradePath.PathB:
                    damage = nextStep switch
                    {
                        1 => 1.2f,
                        2 => 1.6f,
                        _ => 2.0f
                    };
                    break;
            }
        }

        protected override string GetUpgradeTitle(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "Lantern Choir I",
                WeaponUpgradePath.PathA when nextStep == 2 => "Lantern Choir II",
                WeaponUpgradePath.PathA => "Lantern Choir III",
                WeaponUpgradePath.PathB when nextStep == 1 => "Baleful Wick I",
                WeaponUpgradePath.PathB when nextStep == 2 => "Baleful Wick II",
                _ => "Baleful Wick III"
            };
        }

        protected override string GetUpgradeDescription(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => FlatCountDelta(1, "Pulse"),
                WeaponUpgradePath.PathA when nextStep == 2 => FlatCountDelta(1, "Pulse"),
                WeaponUpgradePath.PathA => FlatCountDelta(1, "Pulse"),
                WeaponUpgradePath.PathB when nextStep == 1 => FlatDamageDelta(0.4f),
                WeaponUpgradePath.PathB when nextStep == 2 => FlatDamageDelta(0.4f),
                _ => FlatDamageDelta(0.4f)
            };
        }

        protected override void ApplyOverflowUpgrade(WeaponUpgradePath path, int nextStep)
        {
            if (path == WeaponUpgradePath.PathA)
            {
                ModifyProjectileCount(1);
                return;
            }

            ModifyProjectileDamage(0.4f);
        }

        protected override string GetOverflowUpgradeDescription(WeaponUpgradePath path, int nextStep)
        {
            return path == WeaponUpgradePath.PathA
                ? FlatCountDelta(1, "Pulse")
                : FlatDamageDelta(0.4f);
        }

        private IEnumerator PulseSequence()
        {
            for (var i = 0; i < pulseCount; i++)
            {
                Pulse();
                if (pulseCount > 1 && i < pulseCount - 1)
                {
                    yield return new WaitForSeconds(0.12f);
                }
            }
        }

        private void Pulse()
        {
            if (showRadiusIndicator)
            {
                RotLanternRadiusVisual.Spawn(
                    transform.position,
                    radius,
                    radiusIndicatorHeightOffset,
                    radiusIndicatorThickness,
                    radiusIndicatorTint,
                    radiusIndicatorDuration,
                    radiusIndicatorEndScaleMultiplier,
                    radiusIndicatorSortingOrder);
            }

            var colliders = Physics.OverlapSphere(transform.position, radius, enemyMask, QueryTriggerInteraction.Collide);
            resolvedTargets.Clear();
            foreach (var hit in colliders)
            {
                if (hit.transform == transform)
                {
                    continue;
                }

                if (!CombatResolver.TryResolveUniqueHit(hit, CombatTeam.Player, gameObject, resolvedTargets, out var resolvedHit))
                {
                    continue;
                }

                CombatResolver.TryApplyDamage(resolvedHit, gameObject, damage, CombatTeam.Player);
            }

            PlaceholderWeaponVisual.Spawn(
                "RotLanternPulse",
                transform.position + Vector3.up * 0.18f,
                new Vector3(radius * 0.55f, radius * 0.55f, 1f),
                worldCamera,
                new Color(0.72f, 0.96f, 0.72f, 0.5f),
                0.24f,
                1.18f,
                spritePath: LanternSpritePath,
                pixelsPerUnit: 256f);
        }
    }
}
