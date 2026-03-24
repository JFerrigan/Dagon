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
        private const float BaseCorruptionGainMultiplier = 0.6f;

        [SerializeField] private float maxCorruption = 250f;
        [SerializeField] private float[] thresholdValues = { 25f, 50f, 75f, 100f, 125f, 150f, 175f, 200f, 225f, 250f };
        [SerializeField] private CorruptionThresholdEvent onThresholdReached;

        private float currentCorruption;
        private int highestThresholdIndex = -1;
        private float corruptionGainMultiplier = BaseCorruptionGainMultiplier;

        public event Action<int> ThresholdReached;
        public event Action<int, int> StageChanged;

        public float CurrentCorruption => currentCorruption;
        public float MaxCorruption => maxCorruption;
        public float NormalizedCorruption => maxCorruption <= 0f ? 0f : currentCorruption / maxCorruption;
        public float CorruptionGainMultiplier => corruptionGainMultiplier;
        public int CurrentStageIndex => GetStageIndexForValue(currentCorruption);
        public float[] ThresholdValues => thresholdValues;

        public void AddCorruption(float amount)
        {
            if (amount <= 0f || maxCorruption <= 0f)
            {
                return;
            }

            SetCorruptionInternal(currentCorruption + (amount * corruptionGainMultiplier));
        }

        public void ReduceCorruption(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            SetCorruptionInternal(currentCorruption - amount);
        }

        public void SetCorruption(float amount)
        {
            SetCorruptionInternal(amount);
        }

        public void ResetMeter()
        {
            var previousStageIndex = CurrentStageIndex;
            currentCorruption = 0f;
            highestThresholdIndex = -1;
            corruptionGainMultiplier = BaseCorruptionGainMultiplier;
            if (previousStageIndex != -1)
            {
                StageChanged?.Invoke(previousStageIndex, -1);
            }
        }

        public void SetCorruptionGainMultiplier(float multiplier)
        {
            corruptionGainMultiplier = Mathf.Max(0f, multiplier);
        }

        public float GetThresholdValue(int thresholdIndex)
        {
            if (thresholdValues == null || thresholdIndex < 0 || thresholdIndex >= thresholdValues.Length)
            {
                return maxCorruption;
            }

            return thresholdValues[thresholdIndex];
        }

        private void SetCorruptionInternal(float amount)
        {
            var previousStageIndex = CurrentStageIndex;
            currentCorruption = Mathf.Max(0f, amount);
            RefreshThresholds();
            var nextStageIndex = CurrentStageIndex;
            if (previousStageIndex != nextStageIndex)
            {
                StageChanged?.Invoke(previousStageIndex, nextStageIndex);
            }
        }

        private void RefreshThresholds()
        {
            var nextHighestThresholdIndex = -1;
            for (var i = 0; i < thresholdValues.Length; i++)
            {
                if (currentCorruption < thresholdValues[i])
                {
                    break;
                }

                nextHighestThresholdIndex = i;
            }

            if (nextHighestThresholdIndex <= highestThresholdIndex)
            {
                highestThresholdIndex = nextHighestThresholdIndex;
                return;
            }

            for (var i = highestThresholdIndex + 1; i <= nextHighestThresholdIndex; i++)
            {
                onThresholdReached?.Invoke(i);
                ThresholdReached?.Invoke(i);
            }

            highestThresholdIndex = nextHighestThresholdIndex;
        }

        private int GetStageIndexForValue(float value)
        {
            if (thresholdValues == null || thresholdValues.Length == 0)
            {
                return -1;
            }

            for (var i = thresholdValues.Length - 1; i >= 0; i--)
            {
                if (value >= thresholdValues[i])
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
