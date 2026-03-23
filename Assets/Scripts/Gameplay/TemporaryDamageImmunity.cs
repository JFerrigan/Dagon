using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class TemporaryDamageImmunity : MonoBehaviour
    {
        private float remainingDuration;

        public bool IsActive => remainingDuration > 0f;

        private void Update()
        {
            if (remainingDuration > 0f)
            {
                remainingDuration = Mathf.Max(0f, remainingDuration - Time.deltaTime);
            }
        }

        public void Grant(float duration)
        {
            remainingDuration = Mathf.Max(remainingDuration, Mathf.Max(0f, duration));
        }
    }
}
