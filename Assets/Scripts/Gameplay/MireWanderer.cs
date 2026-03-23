using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MireWanderer : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float wanderRadius = 8f;
        [SerializeField] private float moveSpeed = 1.9f;
        [SerializeField] private float stoppingDistance = 0.75f;
        [SerializeField] private float retargetIntervalMin = 1.2f;
        [SerializeField] private float retargetIntervalMax = 2.6f;
        [SerializeField] private EnemySlowReceiver slowReceiver;
        [SerializeField] private BodyBlocker bodyBlocker;

        private Vector3 origin;
        private Vector3 roamTarget;
        private float retargetTimer;

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

            origin = transform.position;
            PickRoamTarget();
        }

        private void Update()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                target = player != null ? player.transform : null;
            }

            var destination = ResolveDestination();
            var offset = destination - transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude <= stoppingDistance * stoppingDistance)
            {
                retargetTimer -= Time.deltaTime;
                if (retargetTimer <= 0f)
                {
                    PickRoamTarget();
                }
                return;
            }

            var effectiveSpeed = moveSpeed * (slowReceiver != null ? slowReceiver.SpeedMultiplier : 1f);
            var desiredDelta = offset.normalized * (effectiveSpeed * Time.deltaTime);
            transform.position += bodyBlocker != null
                ? BodyBlockerResolver.ResolvePlanarMovement(bodyBlocker, desiredDelta)
                : desiredDelta;
        }

        public void Configure(Transform newTarget, float newMoveSpeed, float newWanderRadius, float newChaseDistance)
        {
            target = newTarget;
            moveSpeed = Mathf.Max(0.1f, newMoveSpeed);
            wanderRadius = Mathf.Max(0.5f, newWanderRadius);
            origin = transform.position;
            PickRoamTarget();
        }

        private Vector3 ResolveDestination()
        {
            if (target == null)
            {
                return roamTarget;
            }

            return target.position;
        }

        private void PickRoamTarget()
        {
            var circle = Random.insideUnitCircle * wanderRadius;
            roamTarget = origin + new Vector3(circle.x, 0f, circle.y);
            retargetTimer = Random.Range(retargetIntervalMin, retargetIntervalMax);
        }
    }
}
