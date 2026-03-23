using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DrownedAcolyteShooter : MonoBehaviour
    {
        private enum State
        {
            Reposition,
            Windup,
            Recover
        }

        [SerializeField] private Transform target;
        [SerializeField] private DrownedAcolyteProjectile projectilePrefab;
        [SerializeField] private float moveSpeed = 2.6f;
        [SerializeField] private float preferredRange = 6f;
        [SerializeField] private float fireCooldown = 1.6f;
        [SerializeField] private float windupDuration = 0.42f;
        [SerializeField] private float recoveryDuration = 0.28f;
        [SerializeField] private float castMoveSpeedMultiplier = 0.42f;
        [SerializeField] private float projectileSpeed = 6.5f;
        [SerializeField] private float projectileDamage = 0.75f;
        [SerializeField] private int volleyCount = 3;
        [SerializeField] private float volleySpread = 24f;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private EnemySlowReceiver slowReceiver;
        [SerializeField] private BodyBlocker bodyBlocker;

        private float fireTimer;
        private float stateTimer;
        private State state;
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

            state = State.Reposition;
        }

        private void Update()
        {
            ResolveReferences();
            if (target == null)
            {
                return;
            }

            var toTarget = target.position - transform.position;
            toTarget.y = 0f;
            var distance = toTarget.magnitude;
            fireTimer -= Time.deltaTime;
            stateTimer -= Time.deltaTime;

            switch (state)
            {
                case State.Reposition:
                    Reposition(toTarget, distance, 1f);
                    TryStartVolley(toTarget, distance);
                    break;
                case State.Windup:
                    Reposition(toTarget, distance, castMoveSpeedMultiplier);
                    if (stateTimer <= 0f)
                    {
                        FireVolley();
                    }
                    break;
                case State.Recover:
                    Reposition(toTarget, distance, castMoveSpeedMultiplier * 0.8f);
                    if (stateTimer <= 0f)
                    {
                        state = State.Reposition;
                    }
                    break;
            }
        }

        public void Configure(
            Transform newTarget,
            DrownedAcolyteProjectile prefab,
            float newMoveSpeed,
            float newPreferredRange,
            float newCooldown,
            Camera cameraReference)
        {
            target = newTarget;
            projectilePrefab = prefab;
            moveSpeed = Mathf.Max(0.1f, newMoveSpeed);
            preferredRange = Mathf.Max(1f, newPreferredRange);
            fireCooldown = Mathf.Max(0.2f, newCooldown);
            worldCamera = cameraReference;
            fireTimer = Random.Range(0.45f, 1.1f);
            state = State.Reposition;
            stateTimer = 0f;
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

        private void Reposition(Vector3 toTarget, float distance, float moveMultiplier)
        {
            if (distance <= 0.01f)
            {
                return;
            }

            var effectiveSpeed = moveSpeed * moveMultiplier * (slowReceiver != null ? slowReceiver.SpeedMultiplier : 1f);
            if (distance > preferredRange + 0.75f)
            {
                ApplyMovement(toTarget.normalized * (effectiveSpeed * Time.deltaTime));
            }
            else if (distance < preferredRange - 0.65f)
            {
                ApplyMovement(-toTarget.normalized * (effectiveSpeed * Time.deltaTime));
            }
        }

        private void ApplyMovement(Vector3 desiredDelta)
        {
            transform.position += bodyBlocker != null
                ? BodyBlockerResolver.ResolvePlanarMovement(bodyBlocker, desiredDelta)
                : desiredDelta;
        }

        private void TryStartVolley(Vector3 toTarget, float distance)
        {
            if (fireTimer > 0f || projectilePrefab == null || distance <= 0.25f)
            {
                return;
            }

            queuedAimDirection = toTarget.normalized;
            state = State.Windup;
            stateTimer = windupDuration;

            var yaw = Mathf.Atan2(queuedAimDirection.x, queuedAimDirection.z) * Mathf.Rad2Deg;
            PlaceholderWeaponVisual.Spawn(
                "AcolyteVolleyWindup",
                transform.position + Vector3.up * 0.16f,
                new Vector3(1.1f, 1.1f, 1f),
                worldCamera,
                new Color(0.52f, 0.92f, 0.60f, 0.42f),
                windupDuration,
                1.06f,
                yaw,
                spritePath: "Sprites/Effects/brine_surge",
                pixelsPerUnit: 256f,
                sortingOrder: 9,
                groundPlane: false);
        }

        private void FireVolley()
        {
            var count = Mathf.Max(1, volleyCount);
            var startAngle = count > 1 ? -volleySpread * 0.5f : 0f;
            var angleStep = count > 1 ? volleySpread / (count - 1) : 0f;

            for (var i = 0; i < count; i++)
            {
                var yaw = startAngle + (angleStep * i);
                var direction = Quaternion.AngleAxis(yaw, Vector3.up) * queuedAimDirection;
                SpawnProjectile(direction.normalized);
            }

            fireTimer = fireCooldown;
            state = State.Recover;
            stateTimer = recoveryDuration;
        }

        private void SpawnProjectile(Vector3 direction)
        {
            var projectile = Instantiate(
                projectilePrefab,
                transform.position + Vector3.up * 0.38f,
                Quaternion.LookRotation(direction, Vector3.up));
            projectile.gameObject.SetActive(true);
            projectile.Initialize(
                gameObject,
                direction,
                projectileSpeed,
                projectileDamage);
        }
    }
}
