using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ParasiteChaser : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float moveSpeed = 7.8f;
        [SerializeField] private float stoppingDistance = 0.25f;
        [SerializeField] private EnemySlowReceiver slowReceiver;
        [SerializeField] private BodyBlocker bodyBlocker;

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

            var offset = target.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= stoppingDistance * stoppingDistance)
            {
                return;
            }

            var effectiveSpeed = moveSpeed * (slowReceiver != null ? slowReceiver.SpeedMultiplier : 1f);
            var desiredDelta = offset.normalized * (effectiveSpeed * Time.deltaTime);
            transform.position += bodyBlocker != null
                ? BodyBlockerResolver.ResolvePlanarMovement(bodyBlocker, desiredDelta)
                : desiredDelta;
        }

        public void Configure(Transform newTarget, float newMoveSpeed, float newStoppingDistance = 0.25f)
        {
            target = newTarget;
            moveSpeed = Mathf.Max(0.1f, newMoveSpeed);
            stoppingDistance = Mathf.Max(0f, newStoppingDistance);
        }

        public void ApplyCorruptionModifiers(float speedMultiplier)
        {
            moveSpeed = Mathf.Max(0.1f, moveSpeed * Mathf.Max(0.1f, speedMultiplier));
        }
    }
}
