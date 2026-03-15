using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CorruptionRuntimeEffects : MonoBehaviour
    {
        [SerializeField] private CorruptionMeter corruptionMeter;
        [SerializeField] private HarpoonLauncher harpoonLauncher;
        [SerializeField] private BrineSurgeAbility brineSurgeAbility;
        [SerializeField] private SpawnDirector spawnDirector;

        private void Awake()
        {
            if (corruptionMeter == null)
            {
                corruptionMeter = GetComponent<CorruptionMeter>();
            }

            if (harpoonLauncher == null)
            {
                harpoonLauncher = GetComponent<HarpoonLauncher>();
            }

            if (brineSurgeAbility == null)
            {
                brineSurgeAbility = GetComponent<BrineSurgeAbility>();
            }
        }

        private void OnEnable()
        {
            if (corruptionMeter != null)
            {
                corruptionMeter.ThresholdReached += HandleThresholdReached;
            }
        }

        private void OnDisable()
        {
            if (corruptionMeter != null)
            {
                corruptionMeter.ThresholdReached -= HandleThresholdReached;
            }
        }

        public void Configure(SpawnDirector director)
        {
            spawnDirector = director;
        }

        private void HandleThresholdReached(int thresholdIndex)
        {
            switch (thresholdIndex)
            {
                case 0:
                    harpoonLauncher?.ModifyAttacksPerSecond(0.2f);
                    break;
                case 1:
                    brineSurgeAbility?.ModifyRadius(0.75f);
                    spawnDirector?.TightenPressure(0.12f, 3);
                    break;
                case 2:
                    harpoonLauncher?.ModifyProjectileDamage(0.8f);
                    spawnDirector?.TightenPressure(0.1f, 5);
                    break;
            }
        }
    }
}
