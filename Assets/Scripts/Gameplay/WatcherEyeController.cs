using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class WatcherEyeController : MonoBehaviour
    {
        private enum State
        {
            Hover,
            Windup,
            Recover
        }

        private enum Attack
        {
            None,
            OrbLance,
            EyeMark
        }

        [SerializeField] private Transform target;
        [SerializeField] private HarpoonProjectile orbProjectilePrefab;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private float hoverSpeed = 2.35f;
        [SerializeField] private float preferredRange = 7.8f;
        [SerializeField] private float closeRange = 4.8f;
        [SerializeField] private float hoverStrafeStrength = 1.1f;
        [SerializeField] private float orbCooldown = 2.2f;
        [SerializeField] private float orbWindup = 0.35f;
        [SerializeField] private float orbSpeed = 7.8f;
        [SerializeField] private float orbDamage = 0.9f;
        [SerializeField] private float markCooldown = 4.4f;
        [SerializeField] private float markWindup = 0.55f;
        [SerializeField] private float markRadius = 1.2f;
        [SerializeField] private float markDamage = 1.4f;
        [SerializeField] private float markDelay = 0.65f;
        [SerializeField] private float markLingerDuration = 0.22f;
        [SerializeField] private float recoveryDuration = 0.4f;
        [SerializeField] private EnemySlowReceiver slowReceiver;

        private float orbCooldownTimer;
        private float markCooldownTimer;
        private float stateTimer;
        private float hoverDirection = 1f;
        private float hoverSwapTimer;
        private State state;
        private Attack queuedAttack;
        private Vector3 queuedAimDirection = Vector3.forward;
        private Vector3 queuedMarkPosition;

        private void Awake()
        {
            if (slowReceiver == null)
            {
                slowReceiver = GetComponent<EnemySlowReceiver>();
            }

            hoverSwapTimer = Random.Range(0.7f, 1.4f);
            hoverDirection = Random.value < 0.5f ? -1f : 1f;
            state = State.Hover;
        }

        private void Update()
        {
            ResolveReferences();
            if (target == null)
            {
                return;
            }

            orbCooldownTimer = Mathf.Max(0f, orbCooldownTimer - Time.deltaTime);
            markCooldownTimer = Mathf.Max(0f, markCooldownTimer - Time.deltaTime);
            stateTimer -= Time.deltaTime;

            var toTarget = target.position - transform.position;
            toTarget.y = 0f;

            switch (state)
            {
                case State.Hover:
                    Hover(toTarget);
                    TryQueueAttack(toTarget);
                    break;
                case State.Windup:
                    if (stateTimer <= 0f)
                    {
                        ExecuteQueuedAttack();
                    }
                    break;
                case State.Recover:
                    Hover(toTarget * 0.35f);
                    if (stateTimer <= 0f)
                    {
                        state = State.Hover;
                    }
                    break;
            }
        }

        public void Configure(Transform newTarget, HarpoonProjectile projectilePrefab, Camera cameraReference, float newHoverSpeed, float newPreferredRange)
        {
            target = newTarget;
            orbProjectilePrefab = projectilePrefab;
            worldCamera = cameraReference;
            hoverSpeed = Mathf.Max(0.1f, newHoverSpeed);
            preferredRange = Mathf.Max(4f, newPreferredRange);
            closeRange = Mathf.Max(2f, preferredRange - 3f);
            orbCooldownTimer = Random.Range(0.8f, 1.6f);
            markCooldownTimer = Random.Range(1.3f, 2.2f);
            state = State.Hover;
            queuedAttack = Attack.None;
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

        private void Hover(Vector3 toTarget)
        {
            if (toTarget.sqrMagnitude <= 0.01f)
            {
                return;
            }

            hoverSwapTimer -= Time.deltaTime;
            if (hoverSwapTimer <= 0f)
            {
                hoverSwapTimer = Random.Range(0.8f, 1.5f);
                hoverDirection *= -1f;
            }

            var distance = toTarget.magnitude;
            var forward = toTarget.normalized;
            var right = new Vector3(-forward.z, 0f, forward.x);
            var desiredVelocity = right * (hoverDirection * hoverStrafeStrength);

            if (distance > preferredRange + 0.75f)
            {
                desiredVelocity += forward;
            }
            else if (distance < closeRange)
            {
                desiredVelocity -= forward * 1.2f;
            }
            else if (distance < preferredRange - 0.4f)
            {
                desiredVelocity -= forward * 0.5f;
            }

            if (desiredVelocity.sqrMagnitude <= 0.001f)
            {
                return;
            }

            var effectiveSpeed = hoverSpeed * (slowReceiver != null ? slowReceiver.SpeedMultiplier : 1f);
            transform.position += desiredVelocity.normalized * (effectiveSpeed * Time.deltaTime);
        }

        private void TryQueueAttack(Vector3 toTarget)
        {
            var distance = toTarget.magnitude;
            if (orbCooldownTimer <= 0f && distance <= preferredRange + 1.2f)
            {
                QueueAttack(Attack.OrbLance, orbWindup, toTarget.normalized, target.position);
                return;
            }

            if (markCooldownTimer <= 0f && distance <= preferredRange + 0.8f)
            {
                var predicted = target.position;
                var mover = target.GetComponent<PlayerMover>();
                if (mover != null && mover.MoveDirection.sqrMagnitude > 0.01f)
                {
                    predicted += mover.MoveDirection.normalized * 0.95f;
                }

                QueueAttack(Attack.EyeMark, markWindup, toTarget.normalized, predicted);
            }
        }

        private void QueueAttack(Attack attack, float windup, Vector3 aimDirection, Vector3 markedPosition)
        {
            queuedAttack = attack;
            queuedAimDirection = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : transform.forward;
            queuedMarkPosition = markedPosition;
            state = State.Windup;
            stateTimer = Mathf.Max(0.05f, windup);

            PlaceholderWeaponVisual.Spawn(
                attack == Attack.OrbLance ? "WatcherEyeOrbWindup" : "WatcherEyeMarkWindup",
                transform.position + Vector3.up * 0.2f,
                attack == Attack.OrbLance ? new Vector3(0.9f, 0.9f, 1f) : new Vector3(1.15f, 1.15f, 1f),
                worldCamera,
                attack == Attack.OrbLance ? new Color(0.84f, 0.98f, 0.84f, 0.42f) : new Color(0.78f, 0.94f, 0.72f, 0.32f),
                windup,
                1.1f,
                0f,
                spritePath: "Sprites/Enemies/watcher_eye",
                pixelsPerUnit: 256f,
                sortingOrder: 10);
        }

        private void ExecuteQueuedAttack()
        {
            switch (queuedAttack)
            {
                case Attack.OrbLance:
                    FireOrbLance();
                    orbCooldownTimer = orbCooldown;
                    break;
                case Attack.EyeMark:
                    DropEyeMark();
                    markCooldownTimer = markCooldown;
                    break;
            }

            queuedAttack = Attack.None;
            state = State.Recover;
            stateTimer = recoveryDuration;
        }

        private void FireOrbLance()
        {
            if (orbProjectilePrefab == null)
            {
                return;
            }

            var projectile = Instantiate(
                orbProjectilePrefab,
                transform.position + Vector3.up * 0.4f,
                Quaternion.LookRotation(queuedAimDirection, Vector3.up));
            projectile.gameObject.SetActive(true);
            projectile.Initialize(gameObject, queuedAimDirection, orbSpeed, orbDamage);
        }

        private void DropEyeMark()
        {
            queuedMarkPosition.y = transform.position.y;
            WatcherEyeMarkZone.Spawn(
                queuedMarkPosition,
                markRadius,
                markDelay,
                markDamage,
                markLingerDuration,
                worldCamera,
                gameObject);
        }
    }
}
