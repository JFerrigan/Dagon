using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class KnockbackReceiver : MonoBehaviour
    {
        [SerializeField] private float strengthMultiplier = 1f;
        [SerializeField] private float damping = 16f;
        [SerializeField] private float maxSpeed = 6f;

        private Vector3 velocity;
        private float externalStrengthMultiplier = 1f;

        private void Update()
        {
            if (velocity.sqrMagnitude <= 0.0001f)
            {
                velocity = Vector3.zero;
                return;
            }

            transform.position += velocity * Time.deltaTime;
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, damping * Time.deltaTime);
        }

        public void Configure(float newStrengthMultiplier, float newDamping = 16f, float newMaxSpeed = 6f)
        {
            strengthMultiplier = Mathf.Max(0f, newStrengthMultiplier);
            damping = Mathf.Max(0.1f, newDamping);
            maxSpeed = Mathf.Max(0.1f, newMaxSpeed);
        }

        public void SetExternalStrengthMultiplier(float multiplier)
        {
            externalStrengthMultiplier = Mathf.Max(0f, multiplier);
        }

        public void ApplyKnockback(Vector3 direction, float strength)
        {
            var resolvedStrengthMultiplier = strengthMultiplier * externalStrengthMultiplier;
            if (resolvedStrengthMultiplier <= 0f || strength <= 0f)
            {
                return;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            velocity += direction.normalized * (strength * resolvedStrengthMultiplier);
            var speed = velocity.magnitude;
            if (speed > maxSpeed)
            {
                velocity = velocity.normalized * maxSpeed;
            }
        }
    }
}
