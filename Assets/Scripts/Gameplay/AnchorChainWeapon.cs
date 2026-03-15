using System.Collections;
using Dagon.Core;
using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    public sealed class AnchorChainWeapon : PlayerWeaponRuntime
    {
        [SerializeField] private PlayerMover playerMover;
        [SerializeField] private float attacksPerSecond = 0.85f;
        [SerializeField] private float damage = 1.8f;
        [SerializeField] private float radius = 2.4f;
        [SerializeField] private float arcAngle = 105f;
        [SerializeField] private float knockbackForce = 4.5f;
        [SerializeField] private float eliteKnockbackMultiplier = 0.35f;
        [SerializeField] private int arcCount = 1;
        [SerializeField] private LayerMask enemyMask = ~0;

        private Camera worldCamera;
        private float cooldownTimer;

        public override string PathAName => "Chain Flurry";
        public override string PathBName => "Heavy Anchor";

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
                WeaponUpgradePath.PathA when nextStep == 1 => "Add a second chain sweep to each attack cycle.",
                WeaponUpgradePath.PathA when nextStep == 2 => "Add a third chain sweep to each attack cycle.",
                WeaponUpgradePath.PathA => "Increase chain sweep damage to 2.2.",
                WeaponUpgradePath.PathB when nextStep == 1 => "Increase chain damage to 2.5.",
                WeaponUpgradePath.PathB when nextStep == 2 => "Increase chain damage to 3.2.",
                _ => "Increase chain damage to 4.0."
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

                var damageable = hit.GetComponentInParent<IDamageable>();
                damageable?.ApplyDamage(damage, gameObject);
                ApplyKnockback(hit.transform, toTarget.normalized);
            }

            var yaw = Mathf.Atan2(baseDirection.x, baseDirection.z) * Mathf.Rad2Deg;
            var offset = baseDirection * (radius * 0.45f) + Vector3.up * 0.2f;
            PlaceholderWeaponVisual.Spawn(
                "AnchorChainSweep",
                transform.position + offset,
                new Vector3(radius * 1.2f, radius * 0.7f, 1f),
                worldCamera,
                new Color(0.70f, 0.84f, 0.76f, 0.72f),
                0.2f,
                1.05f,
                yaw + (sweepIndex * 4f));
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
    }
}
