using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EnemyDeathRewards : MonoBehaviour
    {
        [SerializeField] private int experienceReward = 1;
        [SerializeField] private float corruptionReward = 1.5f;
        [SerializeField] private Health health;

        private Camera worldCamera;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }
            worldCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
            }
        }

        public void Configure(int newExperienceReward, float newCorruptionReward)
        {
            experienceReward = Mathf.Max(0, newExperienceReward);
            corruptionReward = Mathf.Max(0f, newCorruptionReward);
        }

        private void HandleDeath(Health _, GameObject source)
        {
            ExperiencePickup.Create(transform.position, experienceReward, corruptionReward, worldCamera);
        }
    }
}
