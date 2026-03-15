using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SimpleEnemyChaser : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float stoppingDistance = 0.8f;

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

            transform.position += offset.normalized * (moveSpeed * Time.deltaTime);
        }

        public void Configure(float newMoveSpeed, float newStoppingDistance)
        {
            moveSpeed = Mathf.Max(0.01f, newMoveSpeed);
            stoppingDistance = Mathf.Max(0f, newStoppingDistance);
        }
    }
}
