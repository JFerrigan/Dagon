using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EnemySlowReceiver : MonoBehaviour
    {
        private float slowMultiplier = 1f;
        private float slowTimer;

        public float SpeedMultiplier => slowTimer > 0f ? slowMultiplier : 1f;

        private void Update()
        {
            if (slowTimer <= 0f)
            {
                return;
            }

            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
            {
                slowTimer = 0f;
                slowMultiplier = 1f;
            }
        }

        public void ApplySlow(float amount, float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            var multiplier = Mathf.Clamp01(1f - Mathf.Clamp01(amount));
            if (slowTimer <= 0f || multiplier < slowMultiplier)
            {
                slowMultiplier = multiplier;
            }

            slowTimer = Mathf.Max(slowTimer, duration);
        }
    }
}
