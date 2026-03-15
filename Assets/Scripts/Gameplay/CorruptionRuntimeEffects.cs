using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CorruptionRuntimeEffects : MonoBehaviour
    {
        [SerializeField] private CorruptionMeter corruptionMeter;
        [SerializeField] private PlayerCombatLoadout combatLoadout;
        [SerializeField] private SpawnDirector spawnDirector;

        private void Awake()
        {
            if (corruptionMeter == null)
            {
                corruptionMeter = GetComponent<CorruptionMeter>();
            }

            if (combatLoadout == null)
            {
                combatLoadout = GetComponent<PlayerCombatLoadout>();
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
                    combatLoadout?.ModifyAllWeaponsAttackRate(0.2f);
                    break;
                case 1:
                    combatLoadout?.GetActive(0)?.ModifyRadius(0.75f);
                    spawnDirector?.TightenPressure(0.12f, 3);
                    break;
                case 2:
                    combatLoadout?.ModifyAllWeaponsDamage(0.8f);
                    spawnDirector?.TightenPressure(0.1f, 5);
                    break;
            }
        }
    }
}
