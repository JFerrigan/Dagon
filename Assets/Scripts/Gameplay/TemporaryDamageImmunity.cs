using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class TemporaryDamageImmunity : MonoBehaviour
    {
        private float remainingDuration;
        private bool wasActive;

        public bool IsActive => remainingDuration > 0f;
        public float RemainingDuration => remainingDuration;

        public event System.Action<TemporaryDamageImmunity, float> Activated;
        public event System.Action<TemporaryDamageImmunity, float> Refreshed;
        public event System.Action<TemporaryDamageImmunity> Ended;

        private void Update()
        {
            if (remainingDuration > 0f)
            {
                remainingDuration = Mathf.Max(0f, remainingDuration - Time.deltaTime);
                if (remainingDuration <= 0f && wasActive)
                {
                    wasActive = false;
                    Ended?.Invoke(this);
                }
            }
        }

        public void Grant(float duration)
        {
            var grantedDuration = Mathf.Max(0f, duration);
            if (grantedDuration <= 0f)
            {
                return;
            }

            var previousDuration = remainingDuration;
            remainingDuration = Mathf.Max(remainingDuration, grantedDuration);

            if (!wasActive && remainingDuration > 0f)
            {
                wasActive = true;
                Activated?.Invoke(this, remainingDuration);
                return;
            }

            if (remainingDuration > previousDuration)
            {
                Refreshed?.Invoke(this, remainingDuration);
            }
        }
    }
}
