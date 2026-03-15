using UnityEngine;
using UnityEngine.Events;
using System;

namespace Dagon.Core
{
    [System.Serializable]
    public sealed class CorruptionThresholdEvent : UnityEvent<int>
    {
    }

    [DisallowMultipleComponent]
    public sealed class CorruptionMeter : MonoBehaviour
    {
        [SerializeField] private float maxCorruption = 100f;
        [SerializeField] private float[] thresholdValues = { 25f, 50f, 75f, 100f };
        [SerializeField] private CorruptionThresholdEvent onThresholdReached;

        private float currentCorruption;
        private int highestThresholdIndex = -1;

        public event Action<int> ThresholdReached;

        public float CurrentCorruption => currentCorruption;
        public float MaxCorruption => maxCorruption;
        public float NormalizedCorruption => maxCorruption <= 0f ? 0f : currentCorruption / maxCorruption;

        public void AddCorruption(float amount)
        {
            if (amount <= 0f || maxCorruption <= 0f)
            {
                return;
            }

            currentCorruption = Mathf.Clamp(currentCorruption + amount, 0f, maxCorruption);
            RefreshThresholds();
        }

        public void SetCorruption(float amount)
        {
            currentCorruption = Mathf.Clamp(amount, 0f, maxCorruption);
            RefreshThresholds();
        }

        public void ResetMeter()
        {
            currentCorruption = 0f;
            highestThresholdIndex = -1;
        }

        private void RefreshThresholds()
        {
            for (var i = highestThresholdIndex + 1; i < thresholdValues.Length; i++)
            {
                if (currentCorruption < thresholdValues[i])
                {
                    break;
                }

                highestThresholdIndex = i;
                onThresholdReached?.Invoke(i);
                ThresholdReached?.Invoke(i);
            }
        }
    }
}
