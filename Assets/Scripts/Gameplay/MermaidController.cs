using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MermaidController : MonoBehaviour, IAuraMoveSpeedTarget, IAuraCadenceTarget
    {
        private enum State
        {
            Reposition,
            Windup,
            Recover
        }

        private enum CastAbility
        {
            None,
            AcidPuddle,
            SirenCall
        }

        [SerializeField] private Transform target;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private float moveSpeed = 2.25f;
        [SerializeField] private float retreatSpeed = 2.9f;
        [SerializeField] private float preferredRange = 7f;
        [SerializeField] private float closeRange = 4.25f;
        [SerializeField] private float strafeStrength = 0.9f;
        [SerializeField] private float sirenRange = 6.8f;
        [SerializeField] private float sirenAngle = 48f;
        [SerializeField] private float sirenWindupDuration = 0.7f;
        [SerializeField] private float sirenCooldown = 4.5f;
        [SerializeField] private float sirenDamage = 1f;
        [SerializeField] private float sirenPullStrength = 4f;
        [SerializeField] private float sirenSlowAmount = 0.2f;
        [SerializeField] private float sirenSlowDuration = 0.85f;
        [SerializeField] private float puddleRange = 8.5f;
        [SerializeField] private float puddleRadius = 2.4f;
        [SerializeField] private float puddleWindupDuration = 0.55f;
        [SerializeField] private float puddleCooldown = 3.8f;
        [SerializeField] private float puddleDuration = 3.5f;
        [SerializeField] private float puddleTickDamage = 0.45f;
        [SerializeField] private float puddleTickInterval = 0.45f;
        [SerializeField] private float puddleThrowTravelDuration = 0.48f;
        [SerializeField] private float puddleThrowArcHeight = 1.15f;
        [SerializeField] private float puddleLeadDistance = 1.8f;
        [SerializeField] private float recoveryDuration = 0.55f;
        [SerializeField] private EnemySlowReceiver slowReceiver;
        [SerializeField] private BodyBlocker bodyBlocker;

        private float stateTimer;
        private float sirenCooldownTimer;
        private float strafeDirection = 1f;
        private float strafeSwapTimer;
        private float auraMoveSpeedMultiplier = 1f;
        private float auraCadenceMultiplier = 1f;
        private float puddleCooldownTimer;
        private State state;
        private CastAbility queuedAbility;
        private Vector3 queuedAimDirection = Vector3.forward;
        private Vector3 queuedTargetPoint = Vector3.zero;
        private PlayerMover playerMover;

        private static readonly Color PuddleLanternProjectileTint = new(0.82f, 1f, 0.84f, 0.96f);
        private static readonly Vector3 PuddleLanternProjectileScale = new(0.46f, 0.46f, 1f);
        private const string PuddleLanternSpritePath = "Sprites/Weapons/rot_lantern";

        private void Awake()
        {
            if (slowReceiver == null)
            {
                slowReceiver = GetComponent<EnemySlowReceiver>();
            }

            if (bodyBlocker == null)
            {
                bodyBlocker = GetComponent<BodyBlocker>();
            }

            state = State.Reposition;
            strafeSwapTimer = Random.Range(0.7f, 1.8f);
            strafeDirection = Random.value < 0.5f ? -1f : 1f;
        }

        private void Update()
        {
            ResolveReferences();
            if (target == null)
            {
                return;
            }

            sirenCooldownTimer = Mathf.Max(0f, sirenCooldownTimer - (Time.deltaTime * auraCadenceMultiplier));
            puddleCooldownTimer = Mathf.Max(0f, puddleCooldownTimer - (Time.deltaTime * auraCadenceMultiplier));
            stateTimer -= Time.deltaTime * auraCadenceMultiplier;

            var toTarget = target.position - transform.position;
            toTarget.y = 0f;

            switch (state)
            {
                case State.Reposition:
                    Reposition(toTarget);
                    TryQueueAbility(toTarget);
                    break;
                case State.Windup:
                    if (stateTimer <= 0f)
                    {
                        ExecuteQueuedAbility();
                    }
                    break;
                case State.Recover:
                    Reposition(toTarget * 0.45f);
                    if (stateTimer <= 0f)
                    {
                        state = State.Reposition;
                    }
                    break;
            }
        }

        public void Configure(Transform newTarget, Camera cameraReference, float newMoveSpeed, float newPreferredRange)
        {
            target = newTarget;
            worldCamera = cameraReference;
            moveSpeed = Mathf.Max(0.1f, newMoveSpeed);
            retreatSpeed = Mathf.Max(moveSpeed, newMoveSpeed + 0.65f);
            preferredRange = Mathf.Max(4f, newPreferredRange);
            closeRange = Mathf.Max(2.5f, preferredRange - 2f);
            state = State.Reposition;
            stateTimer = 0f;
            sirenCooldownTimer = Random.Range(1.2f, 2.4f);
            puddleCooldownTimer = Random.Range(0.85f, 1.75f);
            queuedAbility = CastAbility.None;
        }

        public void ApplyCorruptionModifiers(float damageMultiplier, float speedMultiplier, float cadenceMultiplier)
        {
            var safeSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            var safeDamageMultiplier = Mathf.Max(0.1f, damageMultiplier);
            var safeCadenceMultiplier = Mathf.Max(0.1f, cadenceMultiplier);
            moveSpeed = Mathf.Max(0.1f, moveSpeed * safeSpeedMultiplier);
            retreatSpeed = Mathf.Max(moveSpeed, retreatSpeed * safeSpeedMultiplier);
            sirenDamage = Mathf.Max(0.1f, sirenDamage * safeDamageMultiplier);
            puddleTickDamage = Mathf.Max(0.05f, puddleTickDamage * safeDamageMultiplier);
            sirenCooldown = Mathf.Max(0.45f, sirenCooldown / safeCadenceMultiplier);
            sirenWindupDuration = Mathf.Max(0.2f, sirenWindupDuration / safeCadenceMultiplier);
            puddleCooldown = Mathf.Max(0.5f, puddleCooldown / safeCadenceMultiplier);
            puddleWindupDuration = Mathf.Max(0.18f, puddleWindupDuration / safeCadenceMultiplier);
            recoveryDuration = Mathf.Max(0.2f, recoveryDuration / safeCadenceMultiplier);
        }

        private void ResolveReferences()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                target = player != null ? player.transform : null;
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (playerMover == null)
            {
                playerMover = FindFirstObjectByType<PlayerMover>();
            }
        }

        private void Reposition(Vector3 toTarget)
        {
            if (toTarget.sqrMagnitude <= 0.01f)
            {
                return;
            }

            strafeSwapTimer -= Time.deltaTime;
            if (strafeSwapTimer <= 0f)
            {
                strafeSwapTimer = Random.Range(0.8f, 1.7f);
                strafeDirection *= -1f;
            }

            var distance = toTarget.magnitude;
            var forward = toTarget.normalized;
            var right = new Vector3(-forward.z, 0f, forward.x);
            var desiredVelocity = right * (strafeDirection * strafeStrength);

            if (distance > preferredRange + 0.65f)
            {
                desiredVelocity += forward;
            }
            else if (distance < closeRange)
            {
                desiredVelocity -= forward * 1.25f;
            }
            else if (distance < preferredRange - 0.35f)
            {
                desiredVelocity -= forward * 0.55f;
            }

            if (desiredVelocity.sqrMagnitude <= 0.001f)
            {
                return;
            }

            var speed = distance < closeRange ? retreatSpeed : moveSpeed;
            var effectiveSpeed = speed * auraMoveSpeedMultiplier * (slowReceiver != null ? slowReceiver.SpeedMultiplier : 1f);
            ApplyMovement(desiredVelocity.normalized * (effectiveSpeed * Time.deltaTime));
        }

        private void ApplyMovement(Vector3 desiredDelta)
        {
            transform.position += bodyBlocker != null
                ? BodyBlockerResolver.ResolvePlanarMovement(bodyBlocker, desiredDelta)
                : desiredDelta;
        }

        private void TryQueueAbility(Vector3 toTarget)
        {
            var distance = toTarget.magnitude;
            if (puddleCooldownTimer <= 0f && distance <= puddleRange && distance >= closeRange - 1f)
            {
                StartWindup(CastAbility.AcidPuddle, puddleWindupDuration, toTarget.normalized);
                return;
            }

            if (sirenCooldownTimer <= 0f && distance <= sirenRange && distance >= closeRange - 0.75f)
            {
                StartWindup(CastAbility.SirenCall, sirenWindupDuration, toTarget.normalized);
            }
        }

        private void StartWindup(CastAbility ability, float duration, Vector3 aimDirection)
        {
            queuedAbility = ability;
            queuedAimDirection = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : Vector3.forward;
            queuedTargetPoint = ability == CastAbility.AcidPuddle
                ? ResolvePuddleTargetPoint()
                : (target != null ? target.position : transform.position + (queuedAimDirection * puddleRange * 0.75f));
            queuedTargetPoint.y = transform.position.y;
            state = State.Windup;
            stateTimer = Mathf.Max(0.05f, duration);

            if (ability == CastAbility.AcidPuddle)
            {
                SpawnPuddleTelegraph(duration, puddleThrowTravelDuration);
                return;
            }

            var yaw = Mathf.Atan2(queuedAimDirection.x, queuedAimDirection.z) * Mathf.Rad2Deg;
            PlaceholderWeaponVisual.Spawn(
                "MermaidSirenWindup",
                transform.position + Vector3.up * 0.08f,
                new Vector3(2.4f, 2.4f, 1f),
                worldCamera,
                new Color(0.84f, 1f, 0.96f, 0.68f),
                duration,
                1.14f,
                yaw,
                spritePath: "Sprites/Effects/brine_surge",
                pixelsPerUnit: 256f,
                sortingOrder: 9,
                groundPlane: true);

            RotLanternRadiusVisual.Spawn(
                transform.position,
                sirenRange,
                0.05f,
                0.12f,
                new Color(0.84f, 1f, 0.96f, 0.56f),
                duration,
                1.02f,
                8);
        }

        public void SetAuraMoveSpeedMultiplier(float multiplier)
        {
            auraMoveSpeedMultiplier = Mathf.Max(1f, multiplier);
        }

        public void SetAuraCadenceMultiplier(float multiplier)
        {
            auraCadenceMultiplier = Mathf.Max(1f, multiplier);
        }

        private void ExecuteQueuedAbility()
        {
            switch (queuedAbility)
            {
                case CastAbility.AcidPuddle:
                    ExecuteAcidPuddle();
                    puddleCooldownTimer = puddleCooldown;
                    break;
                case CastAbility.SirenCall:
                    ExecuteSirenCall();
                    sirenCooldownTimer = sirenCooldown;
                    break;
            }

            queuedAbility = CastAbility.None;
            state = State.Recover;
            stateTimer = recoveryDuration;
        }

        private void ExecuteSirenCall()
        {
            PlaceholderWeaponVisual.Spawn(
                "MermaidSirenPulse",
                transform.position + Vector3.up * 0.08f,
                new Vector3(3.1f, 3.1f, 1f),
                worldCamera,
                new Color(0.90f, 1f, 0.98f, 0.78f),
                0.42f,
                1.22f,
                spritePath: "Sprites/Effects/brine_surge",
                pixelsPerUnit: 256f,
                sortingOrder: 10,
                groundPlane: true);

            RotLanternRadiusVisual.Spawn(
                transform.position,
                sirenRange,
                0.05f,
                0.18f,
                new Color(0.92f, 1f, 0.98f, 0.72f),
                0.38f,
                1.06f,
                9);

            if (!TryResolvePlayerTarget(out var playerHurtbox, out var playerCollider))
            {
                return;
            }

            var toPlayer = playerHurtbox.transform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude <= 0.01f || toPlayer.magnitude > sirenRange)
            {
                return;
            }

            if (Vector3.Angle(queuedAimDirection, toPlayer.normalized) > sirenAngle * 0.5f)
            {
                return;
            }

            playerHurtbox.Damageable?.ApplyDamage(sirenDamage, gameObject);

            var pullDirection = transform.position - playerHurtbox.transform.position;
            pullDirection.y = 0f;
            if (!CombatKnockback.TryApply(playerCollider, pullDirection.normalized, sirenPullStrength))
            {
                playerHurtbox.transform.position += pullDirection.normalized * (sirenPullStrength * 0.08f);
            }

            playerHurtbox.GetComponentInParent<PlayerSlowReceiver>()?.ApplySlow(sirenSlowAmount, sirenSlowDuration);
        }

        private void ExecuteAcidPuddle()
        {
            RotBeaconBombProjectile.SpawnHostilePool(
                transform.position,
                queuedTargetPoint,
                puddleThrowTravelDuration,
                puddleThrowArcHeight,
                worldCamera,
                gameObject,
                PuddleLanternSpritePath,
                PuddleLanternProjectileTint,
                PuddleLanternProjectileScale,
                10,
                0.12f,
                puddleRadius,
                puddleDuration,
                puddleTickDamage,
                puddleTickInterval,
                0f,
                0.1f,
                "MermaidAcidPuddle");
        }

        private void SpawnPuddleTelegraph(float windupDuration, float flightDuration)
        {
            var toTarget = queuedTargetPoint - transform.position;
            toTarget.y = 0f;
            var yaw = toTarget.sqrMagnitude > 0.001f ? Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg : 0f;
            var landingTelegraphDuration = Mathf.Max(windupDuration, windupDuration + Mathf.Max(0.05f, flightDuration));

            PlaceholderWeaponVisual.Spawn(
                "MermaidPuddleThrowWindup",
                transform.position + Vector3.up * 0.15f,
                new Vector3(1.75f, 1.75f, 1f),
                worldCamera,
                new Color(0.82f, 1f, 0.86f, 0.60f),
                windupDuration,
                1.14f,
                yaw,
                spritePath: "Sprites/Effects/brine_surge",
                pixelsPerUnit: 256f,
                sortingOrder: 9,
                groundPlane: false);

            PlaceholderWeaponVisual.Spawn(
                "MermaidPuddleLandingFill",
                queuedTargetPoint + Vector3.up * 0.05f,
                new Vector3(puddleRadius * 2.12f, puddleRadius * 2.12f, 1f),
                worldCamera,
                new Color(0.44f, 0.94f, 0.60f, 0.42f),
                landingTelegraphDuration,
                1f,
                0f,
                spritePath: "Sprites/Effects/brine_surge",
                pixelsPerUnit: 256f,
                sortingOrder: 4,
                groundPlane: true);

            RotLanternRadiusVisual.Spawn(
                queuedTargetPoint,
                puddleRadius,
                0.04f,
                0.18f,
                new Color(0.88f, 1f, 0.90f, 0.92f),
                landingTelegraphDuration,
                1f,
                5);
        }

        private Vector3 ResolvePuddleTargetPoint()
        {
            return EnemyLobTargeting.ResolveLeadTargetPoint(
                transform.position,
                target,
                playerMover,
                puddleRange,
                puddleLeadDistance,
                queuedAimDirection);
        }

        private bool TryResolvePlayerTarget(out Hurtbox playerHurtbox, out Collider playerCollider)
        {
            playerHurtbox = null;
            playerCollider = null;
            if (target == null)
            {
                return false;
            }

            playerHurtbox = target.GetComponentInParent<Hurtbox>();
            playerCollider = target.GetComponentInParent<Collider>();
            return playerHurtbox != null && playerCollider != null && playerHurtbox.Team == CombatTeam.Player;
        }
    }
}
