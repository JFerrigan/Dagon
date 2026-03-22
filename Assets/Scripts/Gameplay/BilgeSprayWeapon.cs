using System.Collections;
using System.Collections.Generic;
using Dagon.Core;
using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    public sealed class BilgeSprayWeapon : PlayerWeaponRuntime
    {
        private const string BilgeSpraySpritePath = "Sprites/Weapons/bilge_spray";
        private const float SprayReachMultiplier = 0.72f;
        private const float SprayConeMultiplier = 0.8f;

        [SerializeField] private PlayerMover playerMover;
        [SerializeField] private float attacksPerSecond = 0.65f;
        [SerializeField] private float damage = 0.7f;
        [SerializeField] private float range = 3.2f;
        [SerializeField] private float coneAngle = 70f;
        [SerializeField] private float slowAmount = 0.25f;
        [SerializeField] private float slowDuration = 1.5f;
        [SerializeField] private int burstCount = 1;
        [SerializeField] private LayerMask enemyMask = ~0;

        private Camera worldCamera;
        private float cooldownTimer;
        private readonly HashSet<GameObject> resolvedTargets = new();

        public override string PathAName => "Pressure Wash";
        public override string PathBName => "Foul Brine";

        private void Awake()
        {
            if (playerMover == null)
            {
                playerMover = GetComponent<PlayerMover>();
            }
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
            burstCount = Mathf.Max(1, burstCount + amount);
        }

        protected override void ApplyDefinition(WeaponDefinition runtimeDefinition)
        {
            attacksPerSecond = Mathf.Max(0.1f, runtimeDefinition.AttacksPerSecond);
            damage = Mathf.Max(0.1f, runtimeDefinition.ProjectileDamage);
            range = Mathf.Max(0.5f, runtimeDefinition.EffectRadius);
            coneAngle = Mathf.Max(10f, runtimeDefinition.EffectAngle);
            slowAmount = Mathf.Clamp01(runtimeDefinition.SlowAmount);
            slowDuration = Mathf.Max(0f, runtimeDefinition.SlowDuration);
            burstCount = Mathf.Max(1, runtimeDefinition.ProjectilesPerVolley);
        }

        protected override void ApplyPathUpgrade(WeaponUpgradePath path, int nextStep)
        {
            switch (path)
            {
                case WeaponUpgradePath.PathA:
                    if (nextStep <= 2)
                    {
                        burstCount = nextStep + 1;
                    }
                    else
                    {
                        damage = 1.0f;
                    }
                    break;
                case WeaponUpgradePath.PathB:
                    damage = nextStep switch
                    {
                        1 => 1.0f,
                        2 => 1.4f,
                        _ => 1.8f
                    };
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
                WeaponUpgradePath.PathA when nextStep == 1 => "Each spray attack releases two bursts.",
                WeaponUpgradePath.PathA when nextStep == 2 => "Each spray attack releases three bursts.",
                WeaponUpgradePath.PathA => "Increase spray damage to 1.0.",
                WeaponUpgradePath.PathB when nextStep == 1 => "Increase spray damage to 1.0.",
                WeaponUpgradePath.PathB when nextStep == 2 => "Increase spray damage to 1.4.",
                _ => "Increase spray damage to 1.8."
            };
        }

        private IEnumerator FireBurstSequence()
        {
            var aim = ResolveAimDirection();
            for (var i = 0; i < burstCount; i++)
            {
                Spray(aim);
                if (burstCount > 1 && i < burstCount - 1)
                {
                    yield return new WaitForSeconds(0.07f);
                }
            }
        }

        private void Spray(Vector3 aim)
        {
            var sprayReach = range * SprayReachMultiplier;
            var sprayHalfAngle = coneAngle * SprayConeMultiplier * 0.5f;
            var colliders = Physics.OverlapSphere(transform.position, sprayReach, enemyMask, QueryTriggerInteraction.Collide);
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

                if (toTarget.sqrMagnitude > sprayReach * sprayReach)
                {
                    continue;
                }

                if (Vector3.Angle(aim, toTarget.normalized) > sprayHalfAngle)
                {
                    continue;
                }

                if (!CombatResolver.TryResolveUniqueHit(hit, CombatTeam.Player, gameObject, resolvedTargets, out var resolvedHit))
                {
                    continue;
                }

                CombatResolver.TryApplyDamage(resolvedHit, gameObject, damage, CombatTeam.Player);

                var slowReceiver = resolvedHit.TargetRoot != null ? resolvedHit.TargetRoot.GetComponent<EnemySlowReceiver>() : null;
                if (slowReceiver == null && resolvedHit.Hurtbox != null)
                {
                    slowReceiver = resolvedHit.Hurtbox.Health != null ? resolvedHit.Hurtbox.Health.GetComponent<EnemySlowReceiver>() : null;
                    if (slowReceiver == null && resolvedHit.Hurtbox.Health != null)
                    {
                        slowReceiver = resolvedHit.Hurtbox.Health.gameObject.AddComponent<EnemySlowReceiver>();
                    }
                }

                slowReceiver?.ApplySlow(slowAmount, slowDuration);
            }

            var yaw = Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg;
            var effectScale = new Vector3(sprayReach, sprayReach, 1f);
            var spriteOffset = new Vector3(effectScale.x * 0.5f, 0f, effectScale.y * 0.5f);
            PlaceholderWeaponVisual.Spawn(
                "BilgeSpray",
                transform.position + Vector3.up * 0.05f,
                effectScale,
                worldCamera,
                new Color(0.58f, 0.88f, 0.62f, 0.22f),
                0.2f,
                1.04f,
                yaw - 45f,
                spritePath: BilgeSpraySpritePath,
                pixelsPerUnit: 256f,
                sortingOrder: 4,
                groundPlane: true,
                spriteLocalOffset: spriteOffset);
        }

        private Vector3 ResolveAimDirection()
        {
            var aim = playerMover != null ? playerMover.AimDirection : transform.forward;
            return aim.sqrMagnitude > 0.001f ? aim.normalized : transform.forward;
        }
    }
}
