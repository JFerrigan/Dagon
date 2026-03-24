using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DeepSpawnBruiser : MonoBehaviour, IAuraMoveSpeedTarget, IAuraCadenceTarget
    {
        [SerializeField] private Transform target;
        [SerializeField] private float driftSpeed = 1.1f;
        [SerializeField] private float chargeSpeed = 4.2f;
        [SerializeField] private float chargeTriggerDistance = 6.5f;
        [SerializeField] private float windupDuration = 0.55f;
        [SerializeField] private float chargeDuration = 1.2f;
        [SerializeField] private float recoveryDuration = 1.4f;
        [SerializeField] private EnemySlowReceiver slowReceiver;
        [SerializeField] private BodyBlocker bodyBlocker;

        private float stateTimer;
        private float auraMoveSpeedMultiplier = 1f;
        private float auraCadenceMultiplier = 1f;
        private State state;

        private enum State
        {
            Drift,
            Windup,
            Charge,
            Recover
        }

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

            stateTimer -= Time.deltaTime * auraCadenceMultiplier;
            var toTarget = target.position - transform.position;
            toTarget.y = 0f;

            switch (state)
            {
                case State.Drift:
                    Drift(toTarget);
                    if (toTarget.sqrMagnitude <= chargeTriggerDistance * chargeTriggerDistance && stateTimer <= 0f)
                    {
                        state = State.Windup;
                        stateTimer = windupDuration;
                    }
                    break;
                case State.Windup:
                    Drift(toTarget * 0.15f);
                    if (stateTimer <= 0f)
                    {
                        state = State.Charge;
                        stateTimer = chargeDuration;
                    }
                    break;
                case State.Charge:
                    Charge(toTarget);
                    if (stateTimer <= 0f)
                    {
                        state = State.Recover;
                        stateTimer = recoveryDuration;
                    }
                    break;
                case State.Recover:
                    Drift(toTarget * 0.55f);
                    if (stateTimer <= 0f)
                    {
                        state = State.Drift;
                        stateTimer = Random.Range(0.4f, 1.1f);
                    }
                    break;
            }
        }

        public void Configure(Transform newTarget, float newDriftSpeed, float newChargeSpeed)
        {
            target = newTarget;
            driftSpeed = Mathf.Max(0.1f, newDriftSpeed);
            chargeSpeed = Mathf.Max(driftSpeed, newChargeSpeed);
            state = State.Drift;
            stateTimer = Random.Range(0.35f, 0.9f);
        }

        private void Drift(Vector3 toTarget)
        {
            if (toTarget.sqrMagnitude <= 0.01f)
            {
                return;
            }

            var effectiveSpeed = driftSpeed * auraMoveSpeedMultiplier * (slowReceiver != null ? slowReceiver.SpeedMultiplier : 1f);
            ApplyMovement(toTarget.normalized * (effectiveSpeed * Time.deltaTime));
        }

        private void Charge(Vector3 toTarget)
        {
            if (toTarget.sqrMagnitude <= 0.01f)
            {
                return;
            }

            var effectiveSpeed = chargeSpeed * auraMoveSpeedMultiplier * (slowReceiver != null ? slowReceiver.SpeedMultiplier : 1f);
            ApplyMovement(toTarget.normalized * (effectiveSpeed * Time.deltaTime));
        }

        private void ApplyMovement(Vector3 desiredDelta)
        {
            transform.position += bodyBlocker != null
                ? BodyBlockerResolver.ResolvePlanarMovement(bodyBlocker, desiredDelta)
                : desiredDelta;
        }

        public void SetAuraMoveSpeedMultiplier(float multiplier)
        {
            auraMoveSpeedMultiplier = Mathf.Max(1f, multiplier);
        }

        public void SetAuraCadenceMultiplier(float multiplier)
        {
            auraCadenceMultiplier = Mathf.Max(1f, multiplier);
        }
    }
}
