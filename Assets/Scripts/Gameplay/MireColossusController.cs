using System.Collections.Generic;
using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MireColossusController : MonoBehaviour
    {
        private enum BossPhase
        {
            Tidecall,
            Tempest,
            Abyssal
        }

        private enum BossAttack
        {
            RadialBurst,
            AimedVolley,
            SpiralBurst,
            CrushingCharge,
            MireSlam,
            CrossBurst
        }

        [SerializeField] private Transform target;
        [SerializeField] private HarpoonProjectile orbProjectilePrefab;
        [SerializeField] private float driftSpeed = 0.95f;
        [SerializeField] private float projectileSpeed = 5.1f;
        [SerializeField] private float projectileDamage = 2f;
        [SerializeField] private int radialBurstCount = 10;
        [SerializeField] private int aimedVolleyCount = 5;
        [SerializeField] private float aimedVolleySpread = 26f;
        [SerializeField] private float chargeWindup = 0.8f;
        [SerializeField] private float chargeSpeed = 6f;
        [SerializeField] private float chargeDuration = 0.9f;
        [SerializeField] private float postChargeRecovery = 1.2f;
        [SerializeField] private float slamWindup = 0.9f;
        [SerializeField] private float slamRadius = 3.2f;
        [SerializeField] private float slamDamage = 2f;
        [SerializeField] private float slamHazardDuration = 3f;
        [SerializeField] private float slamHazardTickDamage = 0.75f;
        [SerializeField] private float slamHazardTickInterval = 0.5f;
        [SerializeField] private Camera worldCamera;

        private Health health;
        private float attackTimer;
        private float stateTimer;
        private Vector3 chargeDirection = Vector3.forward;
        private BossAttack nextAttack = BossAttack.RadialBurst;
        private State state;
        private float spiralStartAngle;
        private float cadenceMultiplier = 1f;
        private readonly HashSet<GameObject> resolvedSlamTargets = new();

        private enum State
        {
            Drift,
            BurstCast,
            VolleyCast,
            SpiralCast,
            CrossCast,
            ChargeWindup,
            Charge,
            Recover,
            SlamWindup
        }

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void Update()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                target = player != null ? player.transform : null;
            }

            if (target == null)
            {
                return;
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            var toTarget = target.position - transform.position;
            toTarget.y = 0f;
            attackTimer -= Time.deltaTime;
            stateTimer -= Time.deltaTime;

            switch (state)
            {
                case State.Drift:
                    Drift(toTarget);
                    if (attackTimer <= 0f)
                    {
                        StartNextAttack(toTarget);
                    }
                    break;
                case State.BurstCast:
                    if (stateTimer <= 0f)
                    {
                        FireBurst();
                        state = State.Drift;
                        attackTimer = AdjustCooldown(2.4f);
                    }
                    break;
                case State.VolleyCast:
                    if (stateTimer <= 0f)
                    {
                        FireAimedVolley();
                        state = State.Drift;
                        attackTimer = AdjustCooldown(2f);
                    }
                    break;
                case State.SpiralCast:
                    if (stateTimer <= 0f)
                    {
                        FireSpiralBurst();
                        state = State.Drift;
                        attackTimer = AdjustCooldown(2.4f);
                    }
                    break;
                case State.CrossCast:
                    if (stateTimer <= 0f)
                    {
                        FireCrossBurst();
                        state = State.Drift;
                        attackTimer = AdjustCooldown(2.1f);
                    }
                    break;
                case State.ChargeWindup:
                    if (stateTimer <= 0f)
                    {
                        state = State.Charge;
                        stateTimer = AdjustCooldown(chargeDuration);
                    }
                    break;
                case State.Charge:
                    transform.position += chargeDirection * (chargeSpeed * Time.deltaTime);
                    if (stateTimer <= 0f)
                    {
                        state = State.Recover;
                        stateTimer = AdjustCooldown(postChargeRecovery);
                    }
                    break;
                case State.Recover:
                    if (stateTimer <= 0f)
                    {
                        state = State.Drift;
                        attackTimer = AdjustCooldown(1.6f);
                    }
                    break;
                case State.SlamWindup:
                    if (stateTimer <= 0f)
                    {
                        PerformSlam();
                        state = State.Drift;
                        attackTimer = AdjustCooldown(2.8f);
                    }
                    break;
            }
        }

        public void Configure(Transform newTarget, HarpoonProjectile projectilePrefab, int difficultyTier = 0)
        {
            target = newTarget;
            orbProjectilePrefab = projectilePrefab;
            worldCamera = Camera.main;
            attackTimer = AdjustCooldown(Mathf.Max(1.1f, 2.1f - (difficultyTier * 0.08f)));
            projectileSpeed = 5.1f + (difficultyTier * 0.12f);
            state = State.Drift;
            nextAttack = BossAttack.RadialBurst;
        }

        public void ApplyCorruptionModifiers(float damageMultiplier, float speedMultiplier, float attackCadenceMultiplier)
        {
            var safeDamageMultiplier = Mathf.Max(0.1f, damageMultiplier);
            var safeSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            cadenceMultiplier = Mathf.Max(0.1f, attackCadenceMultiplier);
            driftSpeed *= safeSpeedMultiplier;
            chargeSpeed *= safeSpeedMultiplier;
            projectileDamage *= safeDamageMultiplier;
            slamDamage *= safeDamageMultiplier;
            slamHazardTickDamage *= safeDamageMultiplier;
        }

        private void Drift(Vector3 toTarget)
        {
            if (toTarget.sqrMagnitude > 0.04f)
            {
                transform.position += toTarget.normalized * (driftSpeed * Time.deltaTime);
            }
        }

        private void StartNextAttack(Vector3 toTarget)
        {
            nextAttack = ResolveNextAttack();
            switch (nextAttack)
            {
                case BossAttack.RadialBurst:
                    state = State.BurstCast;
                    stateTimer = AdjustCooldown(0.25f);
                    break;
                case BossAttack.AimedVolley:
                    state = State.VolleyCast;
                    stateTimer = AdjustCooldown(0.2f);
                    break;
                case BossAttack.SpiralBurst:
                    state = State.SpiralCast;
                    stateTimer = AdjustCooldown(0.18f);
                    break;
                case BossAttack.CrushingCharge:
                    chargeDirection = toTarget.sqrMagnitude > 0.04f ? toTarget.normalized : transform.forward;
                    state = State.ChargeWindup;
                    stateTimer = AdjustCooldown(chargeWindup);
                    PlaceholderWeaponVisual.Spawn(
                        "ColossusChargeWindup",
                        transform.position + chargeDirection * 1.2f + Vector3.up * 0.15f,
                        new Vector3(2.4f, 1.2f, 1f),
                        worldCamera,
                        new Color(0.86f, 0.94f, 0.82f, 0.42f),
                        chargeWindup,
                        1.05f,
                        Mathf.Atan2(chargeDirection.x, chargeDirection.z) * Mathf.Rad2Deg);
                    break;
                case BossAttack.MireSlam:
                    state = State.SlamWindup;
                    stateTimer = AdjustCooldown(slamWindup);
                    PlaceholderWeaponVisual.Spawn(
                        "ColossusSlamTelegraph",
                        transform.position + Vector3.up * 0.12f,
                        new Vector3(slamRadius * 2f, slamRadius * 2f, 1f),
                        worldCamera,
                        new Color(0.45f, 0.82f, 0.48f, 0.34f),
                        slamWindup,
                        1.02f);
                    break;
                case BossAttack.CrossBurst:
                    state = State.CrossCast;
                    stateTimer = AdjustCooldown(0.22f);
                    break;
            }
        }

        private float AdjustCooldown(float baseCooldown)
        {
            return Mathf.Max(0.08f, baseCooldown / Mathf.Max(0.1f, cadenceMultiplier));
        }

        private BossPhase ResolvePhase()
        {
            if (health == null || health.MaxHealth <= 0f)
            {
                return BossPhase.Tidecall;
            }

            var ratio = health.CurrentHealth / health.MaxHealth;
            if (ratio > 0.7f)
            {
                return BossPhase.Tidecall;
            }

            if (ratio > 0.35f)
            {
                return BossPhase.Tempest;
            }

            return BossPhase.Abyssal;
        }

        private BossAttack ResolveNextAttack()
        {
            return ResolvePhase() switch
            {
                BossPhase.Tidecall => nextAttack switch
                {
                    BossAttack.RadialBurst => BossAttack.AimedVolley,
                    BossAttack.AimedVolley => BossAttack.RadialBurst,
                    _ => BossAttack.RadialBurst
                },
                BossPhase.Tempest => nextAttack switch
                {
                    BossAttack.RadialBurst => BossAttack.CrushingCharge,
                    BossAttack.CrushingCharge => BossAttack.SpiralBurst,
                    BossAttack.SpiralBurst => BossAttack.AimedVolley,
                    _ => BossAttack.RadialBurst
                },
                _ => nextAttack switch
                {
                    BossAttack.RadialBurst => BossAttack.CrossBurst,
                    BossAttack.CrossBurst => BossAttack.CrushingCharge,
                    BossAttack.CrushingCharge => BossAttack.MireSlam,
                    BossAttack.MireSlam => BossAttack.SpiralBurst,
                    BossAttack.SpiralBurst => BossAttack.AimedVolley,
                    _ => BossAttack.RadialBurst
                }
            };
        }

        private void FireBurst()
        {
            if (orbProjectilePrefab == null)
            {
                return;
            }

            for (var i = 0; i < radialBurstCount; i++)
            {
                var angle = i * (360f / radialBurstCount);
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var projectile = Instantiate(
                    orbProjectilePrefab,
                    transform.position + Vector3.up * 0.5f,
                    Quaternion.LookRotation(direction, Vector3.up));
                projectile.gameObject.SetActive(true);
                projectile.Initialize(gameObject, direction, projectileSpeed, projectileDamage);
            }
        }

        private void FireAimedVolley()
        {
            if (orbProjectilePrefab == null || target == null)
            {
                return;
            }

            var toTarget = target.position - transform.position;
            toTarget.y = 0f;
            var baseDirection = toTarget.sqrMagnitude > 0.04f ? toTarget.normalized : transform.forward;
            var startAngle = -aimedVolleySpread * 0.5f;
            var step = aimedVolleyCount > 1 ? aimedVolleySpread / (aimedVolleyCount - 1) : 0f;

            for (var i = 0; i < aimedVolleyCount; i++)
            {
                var yaw = startAngle + (step * i);
                var direction = Quaternion.AngleAxis(yaw, Vector3.up) * baseDirection;
                SpawnProjectile(direction, projectileSpeed + 0.4f, projectileDamage);
            }
        }

        private void FireSpiralBurst()
        {
            var spokes = 12;
            spiralStartAngle += 15f;
            for (var i = 0; i < spokes; i++)
            {
                var angle = spiralStartAngle + i * (360f / spokes);
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                SpawnProjectile(direction, projectileSpeed + 0.65f, projectileDamage);
            }
        }

        private void FireCrossBurst()
        {
            var baseAngles = new[] { 0f, 45f, 90f, 135f };
            for (var i = 0; i < baseAngles.Length; i++)
            {
                var directionA = Quaternion.Euler(0f, baseAngles[i], 0f) * Vector3.forward;
                var directionB = Quaternion.Euler(0f, baseAngles[i] + 180f, 0f) * Vector3.forward;
                SpawnProjectile(directionA, projectileSpeed + 0.8f, projectileDamage + 0.5f);
                SpawnProjectile(directionB, projectileSpeed + 0.8f, projectileDamage + 0.5f);
            }
        }

        private void SpawnProjectile(Vector3 direction, float speed, float damage)
        {
            if (orbProjectilePrefab == null)
            {
                return;
            }

            var projectile = Instantiate(
                orbProjectilePrefab,
                transform.position + Vector3.up * 0.5f,
                Quaternion.LookRotation(direction, Vector3.up));
            projectile.gameObject.SetActive(true);
            projectile.Initialize(gameObject, direction, speed, damage);
        }

        private void PerformSlam()
        {
            var colliders = Physics.OverlapSphere(transform.position, slamRadius, ~0, QueryTriggerInteraction.Collide);
            resolvedSlamTargets.Clear();
            for (var i = 0; i < colliders.Length; i++)
            {
                if (!CombatResolver.TryResolveUniqueHit(colliders[i], CombatTeam.Enemy, gameObject, resolvedSlamTargets, out var resolvedHit))
                {
                    continue;
                }

                CombatResolver.TryApplyDamage(resolvedHit, gameObject, slamDamage, CombatTeam.Enemy);
            }

            EnemyHazardZone.SpawnForTeam(
                transform.position,
                slamRadius,
                slamHazardDuration,
                slamHazardTickDamage,
                slamHazardTickInterval,
                worldCamera,
                new Color(0.28f, 0.68f, 0.36f, 0.42f),
                CombatTeam.Player,
                gameObject,
                "ColossusSlamHazard");
        }
    }
}
