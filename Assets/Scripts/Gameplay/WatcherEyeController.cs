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
            OrbLance
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
        [SerializeField] private float recoveryDuration = 0.4f;
        [SerializeField] private EnemySlowReceiver slowReceiver;
        [SerializeField] private BodyBlocker bodyBlocker;

        private float orbCooldownTimer;
        private float stateTimer;
        private float hoverDirection = 1f;
        private float hoverSwapTimer;
        private State state;
        private Attack queuedAttack;
        private Vector3 queuedAimDirection = Vector3.forward;

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
            state = State.Hover;
            queuedAttack = Attack.None;
        }

        public void ApplyCorruptionModifiers(float damageMultiplier, float speedMultiplier, float cadenceMultiplier)
        {
            var safeSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            var safeDamageMultiplier = Mathf.Max(0.1f, damageMultiplier);
            var safeCadenceMultiplier = Mathf.Max(0.1f, cadenceMultiplier);
            hoverSpeed = Mathf.Max(0.1f, hoverSpeed * safeSpeedMultiplier);
            orbDamage = Mathf.Max(0.1f, orbDamage * safeDamageMultiplier);
            orbCooldown = Mathf.Max(0.3f, orbCooldown / safeCadenceMultiplier);
            orbWindup = Mathf.Max(0.1f, orbWindup / safeCadenceMultiplier);
            recoveryDuration = Mathf.Max(0.12f, recoveryDuration / safeCadenceMultiplier);
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
            ApplyMovement(desiredVelocity.normalized * (effectiveSpeed * Time.deltaTime));
        }

        private void ApplyMovement(Vector3 desiredDelta)
        {
            transform.position += bodyBlocker != null
                ? BodyBlockerResolver.ResolvePlanarMovement(bodyBlocker, desiredDelta)
                : desiredDelta;
        }

        private void TryQueueAttack(Vector3 toTarget)
        {
            var distance = toTarget.magnitude;
            if (orbCooldownTimer <= 0f && distance <= preferredRange + 1.2f)
            {
                QueueAttack(Attack.OrbLance, orbWindup, toTarget.normalized);
            }
        }

        private void QueueAttack(Attack attack, float windup, Vector3 aimDirection)
        {
            queuedAttack = attack;
            queuedAimDirection = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : transform.forward;
            state = State.Windup;
            stateTimer = Mathf.Max(0.05f, windup);

            PlaceholderWeaponVisual.Spawn(
                "WatcherEyeOrbWindup",
                transform.position + Vector3.up * 0.2f,
                new Vector3(0.9f, 0.9f, 1f),
                worldCamera,
                new Color(0.84f, 0.98f, 0.84f, 0.42f),
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

    }
}
