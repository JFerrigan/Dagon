using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MermaidController : MonoBehaviour
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
            SirenCall,
            BrinePool
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
        [SerializeField] private float sirenDamage = 0.8f;
        [SerializeField] private float sirenPullStrength = 4f;
        [SerializeField] private float sirenSlowAmount = 0.2f;
        [SerializeField] private float sirenSlowDuration = 0.85f;
        [SerializeField] private float brineWindupDuration = 0.45f;
        [SerializeField] private float brineCooldown = 3.8f;
        [SerializeField] private float brineCastRange = 8f;
        [SerializeField] private float brineRadius = 1.55f;
        [SerializeField] private float brineDuration = 2.6f;
        [SerializeField] private float brineTickDamage = 0.45f;
        [SerializeField] private float brineTickInterval = 0.5f;
        [SerializeField] private float brineSlowAmount = 0.28f;
        [SerializeField] private float brineSlowDuration = 1.1f;
        [SerializeField] private float recoveryDuration = 0.55f;
        [SerializeField] private EnemySlowReceiver slowReceiver;

        private float stateTimer;
        private float sirenCooldownTimer;
        private float brineCooldownTimer;
        private float strafeDirection = 1f;
        private float strafeSwapTimer;
        private State state;
        private CastAbility queuedAbility;
        private Vector3 queuedAimDirection = Vector3.forward;
        private Vector3 queuedBrinePosition;

        private void Awake()
        {
            if (slowReceiver == null)
            {
                slowReceiver = GetComponent<EnemySlowReceiver>();
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

            sirenCooldownTimer = Mathf.Max(0f, sirenCooldownTimer - Time.deltaTime);
            brineCooldownTimer = Mathf.Max(0f, brineCooldownTimer - Time.deltaTime);
            stateTimer -= Time.deltaTime;

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
            brineCooldownTimer = Random.Range(0.8f, 1.8f);
            queuedAbility = CastAbility.None;
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
            var effectiveSpeed = speed * (slowReceiver != null ? slowReceiver.SpeedMultiplier : 1f);
            transform.position += desiredVelocity.normalized * (effectiveSpeed * Time.deltaTime);
        }

        private void TryQueueAbility(Vector3 toTarget)
        {
            var distance = toTarget.magnitude;
            if (sirenCooldownTimer <= 0f && distance <= sirenRange && distance >= closeRange - 0.75f)
            {
                StartWindup(CastAbility.SirenCall, sirenWindupDuration, toTarget.normalized, target.position);
                return;
            }

            if (brineCooldownTimer <= 0f && distance <= brineCastRange)
            {
                var predictedPosition = target.position;
                var mover = target.GetComponent<PlayerMover>();
                if (mover != null && mover.MoveDirection.sqrMagnitude > 0.01f)
                {
                    predictedPosition += mover.MoveDirection.normalized * 0.8f;
                }

                StartWindup(CastAbility.BrinePool, brineWindupDuration, toTarget.normalized, predictedPosition);
            }
        }

        private void StartWindup(CastAbility ability, float duration, Vector3 aimDirection, Vector3 targetPosition)
        {
            queuedAbility = ability;
            queuedAimDirection = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : Vector3.forward;
            queuedBrinePosition = targetPosition;
            state = State.Windup;
            stateTimer = Mathf.Max(0.05f, duration);

            var yaw = Mathf.Atan2(queuedAimDirection.x, queuedAimDirection.z) * Mathf.Rad2Deg;
            PlaceholderWeaponVisual.Spawn(
                ability == CastAbility.SirenCall ? "MermaidSirenWindup" : "MermaidBrineWindup",
                transform.position + Vector3.up * 0.15f,
                ability == CastAbility.SirenCall ? new Vector3(1.45f, 1.45f, 1f) : new Vector3(1.1f, 1.1f, 1f),
                worldCamera,
                ability == CastAbility.SirenCall ? new Color(0.72f, 0.90f, 0.86f, 0.42f) : new Color(0.44f, 0.76f, 0.70f, 0.38f),
                duration,
                1.08f,
                yaw,
                spritePath: "Sprites/Effects/brine_surge",
                pixelsPerUnit: 256f,
                sortingOrder: 9,
                groundPlane: ability != CastAbility.SirenCall);
        }

        private void ExecuteQueuedAbility()
        {
            switch (queuedAbility)
            {
                case CastAbility.SirenCall:
                    ExecuteSirenCall();
                    sirenCooldownTimer = sirenCooldown;
                    break;
                case CastAbility.BrinePool:
                    ExecuteBrinePool();
                    brineCooldownTimer = brineCooldown;
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
                transform.position + Vector3.up * 0.2f,
                new Vector3(2f, 2f, 1f),
                worldCamera,
                new Color(0.78f, 0.96f, 0.92f, 0.48f),
                0.32f,
                1.16f,
                spritePath: "Sprites/Effects/brine_surge",
                pixelsPerUnit: 256f,
                sortingOrder: 10);

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

        private void ExecuteBrinePool()
        {
            var clampedPosition = queuedBrinePosition;
            clampedPosition.y = transform.position.y;

            MermaidBrinePool.Spawn(
                clampedPosition,
                brineRadius,
                brineDuration,
                brineTickDamage,
                brineTickInterval,
                brineSlowAmount,
                brineSlowDuration,
                worldCamera,
                gameObject);
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
