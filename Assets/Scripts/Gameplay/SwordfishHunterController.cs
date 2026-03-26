using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SwordfishHunterController : MonoBehaviour, IAuraMoveSpeedTarget, IAuraCadenceTarget
    {
        private enum State
        {
            Orbit,
            DashWindup,
            Dash,
            Recover
        }

        [SerializeField] private Transform target;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private float orbitSpeed = 2.3f;
        [SerializeField] private float dashSpeed = 11.4f;
        [SerializeField] private float orbitRange = 6.4f;
        [SerializeField] private float closeRange = 3.8f;
        [SerializeField] private float dashTriggerRange = 11f;
        [SerializeField] private float dashDistance = 6.6f;
        [SerializeField] private float dashWindupDuration = 0.24f;
        [SerializeField] private float dashDuration = 0.58f;
        [SerializeField] private float chainCooldown = 2.1f;
        [SerializeField] private float recoverDuration = 0.55f;
        [SerializeField] private float trailDamage = 1.05f;
        [SerializeField] private float trailTickInterval = 0.2f;
        [SerializeField] private float trailDuration = 2.6f;
        [SerializeField] private float trailWidth = 0.85f;
        [SerializeField] private EnemySlowReceiver slowReceiver;
        [SerializeField] private BodyBlocker bodyBlocker;

        private float stateTimer;
        private float dashCooldownTimer;
        private float auraMoveSpeedMultiplier = 1f;
        private float auraCadenceMultiplier = 1f;
        private float orbitDirection = 1f;
        private State state;
        private int remainingChainDashes;
        private int totalChainDashes;
        private Vector3 dashDirection = Vector3.forward;
        private Vector3 dashStartPosition;

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

            orbitDirection = Random.value < 0.5f ? -1f : 1f;
            dashCooldownTimer = Random.Range(0.8f, 1.6f);
            state = State.Orbit;
        }

        private void Update()
        {
            ResolveReferences();
            if (target == null)
            {
                return;
            }

            dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - (Time.deltaTime * auraCadenceMultiplier));
            stateTimer -= Time.deltaTime * auraCadenceMultiplier;

            var toTarget = target.position - transform.position;
            toTarget.y = 0f;

            switch (state)
            {
                case State.Orbit:
                    Orbit(toTarget);
                    TryStartDashChain(toTarget);
                    break;
                case State.DashWindup:
                    Orbit(toTarget * 0.12f);
                    if (stateTimer <= 0f)
                    {
                        BeginDash();
                    }
                    break;
                case State.Dash:
                    ContinueDash();
                    break;
                case State.Recover:
                    Orbit(toTarget * 0.35f);
                    if (stateTimer <= 0f)
                    {
                        state = State.Orbit;
                    }
                    break;
            }
        }

        public void Configure(Transform newTarget, Camera cameraReference, float newOrbitSpeed, float newDashSpeed)
        {
            target = newTarget;
            worldCamera = cameraReference;
            orbitSpeed = Mathf.Max(0.1f, newOrbitSpeed);
            dashSpeed = Mathf.Max(orbitSpeed, newDashSpeed);
            dashCooldownTimer = Random.Range(0.8f, 1.6f);
            state = State.Orbit;
            stateTimer = 0f;
            remainingChainDashes = 0;
            totalChainDashes = 0;
        }

        public void ApplyCorruptionModifiers(float damageMultiplier, float speedMultiplier, float cadenceMultiplier)
        {
            var safeDamageMultiplier = Mathf.Max(0.1f, damageMultiplier);
            var safeSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            var safeCadenceMultiplier = Mathf.Max(0.1f, cadenceMultiplier);
            orbitSpeed = Mathf.Max(0.1f, orbitSpeed * safeSpeedMultiplier);
            dashSpeed = Mathf.Max(orbitSpeed, dashSpeed * safeSpeedMultiplier);
            trailDamage = Mathf.Max(0.05f, trailDamage * safeDamageMultiplier);
            dashWindupDuration = Mathf.Max(0.1f, dashWindupDuration / safeCadenceMultiplier);
            dashDuration = Mathf.Max(0.14f, dashDuration / safeCadenceMultiplier);
            chainCooldown = Mathf.Max(0.6f, chainCooldown / safeCadenceMultiplier);
            recoverDuration = Mathf.Max(0.2f, recoverDuration / safeCadenceMultiplier);
        }

        public void SetAuraMoveSpeedMultiplier(float multiplier)
        {
            auraMoveSpeedMultiplier = Mathf.Max(1f, multiplier);
        }

        public void SetAuraCadenceMultiplier(float multiplier)
        {
            auraCadenceMultiplier = Mathf.Max(1f, multiplier);
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

        private void Orbit(Vector3 toTarget)
        {
            if (toTarget.sqrMagnitude <= 0.01f)
            {
                return;
            }

            var distance = toTarget.magnitude;
            var forward = toTarget.normalized;
            var lateral = new Vector3(-forward.z, 0f, forward.x) * orbitDirection;
            var desired = lateral;

            if (distance > orbitRange + 0.55f)
            {
                desired += forward * 0.8f;
            }
            else if (distance < closeRange)
            {
                desired -= forward * 0.85f;
            }
            else if (distance < orbitRange - 0.45f)
            {
                desired -= forward * 0.28f;
            }

            var effectiveSpeed = orbitSpeed * auraMoveSpeedMultiplier * (slowReceiver != null ? slowReceiver.SpeedMultiplier : 1f);
            ApplyMovement(desired.normalized * (effectiveSpeed * Time.deltaTime));
        }

        private void TryStartDashChain(Vector3 toTarget)
        {
            if (dashCooldownTimer > 0f || toTarget.sqrMagnitude > dashTriggerRange * dashTriggerRange)
            {
                return;
            }

            totalChainDashes = RollChainLength();
            remainingChainDashes = totalChainDashes;
            orbitDirection *= -1f;
            QueueNextDash(toTarget);
        }

        private void QueueNextDash(Vector3 toTarget)
        {
            dashDirection = ChooseDashDirection(toTarget);
            state = State.DashWindup;
            stateTimer = dashWindupDuration;
            SpawnDashTelegraph();
        }

        private void BeginDash()
        {
            state = State.Dash;
            stateTimer = dashDuration;
            dashStartPosition = transform.position;
        }

        private void ContinueDash()
        {
            var effectiveSpeed = dashSpeed * auraMoveSpeedMultiplier * (slowReceiver != null ? slowReceiver.SpeedMultiplier : 1f);
            ApplyMovement(dashDirection * (effectiveSpeed * Time.deltaTime));

            if (stateTimer > 0f)
            {
                return;
            }

            SpawnTrail(dashStartPosition, transform.position);
            remainingChainDashes = Mathf.Max(0, remainingChainDashes - 1);
            if (remainingChainDashes > 0)
            {
                var toTarget = target != null ? target.position - transform.position : dashDirection;
                toTarget.y = 0f;
                QueueNextDash(toTarget);
                return;
            }

            state = State.Recover;
            stateTimer = recoverDuration + ((totalChainDashes - 1) * 0.22f);
            dashCooldownTimer = chainCooldown;
        }

        private Vector3 ChooseDashDirection(Vector3 toTarget)
        {
            var forward = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;
            var right = new Vector3(-forward.z, 0f, forward.x);
            var sideSign = Random.value < 0.5f ? -1f : 1f;
            var roll = Random.value;
            if (roll < 0.34f)
            {
                return (right * sideSign).normalized;
            }

            if (roll < 0.58f)
            {
                return ((-forward * 0.82f) + (right * sideSign * 0.56f)).normalized;
            }

            return ((forward * 0.96f) + (right * sideSign * 0.22f)).normalized;
        }

        private int RollChainLength()
        {
            var roll = Random.value;
            if (roll < 0.22f)
            {
                return 1;
            }

            if (roll < 0.68f)
            {
                return 2;
            }

            return 3;
        }

        private void SpawnDashTelegraph()
        {
            PlaceholderWeaponVisual.Spawn(
                "SwordfishDashTelegraph",
                transform.position + (dashDirection * (dashDistance * 0.5f)) + Vector3.up * 0.05f,
                new Vector3(trailWidth * 1.15f, dashDistance, 1f),
                worldCamera,
                new Color(0.78f, 0.96f, 0.92f, 0.42f),
                dashWindupDuration,
                1.03f,
                Mathf.Atan2(dashDirection.x, dashDirection.z) * Mathf.Rad2Deg,
                groundPlane: true);
        }

        private void SpawnTrail(Vector3 start, Vector3 end)
        {
            var delta = end - start;
            delta.y = 0f;
            var length = delta.magnitude;
            if (length <= 0.15f)
            {
                return;
            }

            ShapedHazardZone.SpawnBox(
                start,
                end,
                trailWidth,
                trailDuration,
                trailDamage,
                trailTickInterval,
                worldCamera,
                CombatTeam.Player,
                gameObject,
                new Color(0.18f, 0.78f, 0.86f, 0.42f),
                new Color(0.90f, 1f, 0.98f, 0.3f),
                name: "SwordfishDashTrail");
        }

        private void ApplyMovement(Vector3 desiredDelta)
        {
            transform.position += bodyBlocker != null
                ? BodyBlockerResolver.ResolvePlanarMovement(bodyBlocker, desiredDelta)
                : desiredDelta;
        }
    }
}
