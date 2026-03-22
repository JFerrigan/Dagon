using Dagon.Bootstrap;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class WorldProgressionDirector : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private RunStateManager runStateManager;
        [SerializeField] private SpawnDirector spawnDirector;
        [SerializeField] private MireGroundTiler groundTiler;
        [SerializeField] private MirePropScatterer propScatterer;
        [SerializeField] private float localBiomeRefreshRadius = 22f;

        private RuntimeBiomeProfile[] biomeSequence = System.Array.Empty<RuntimeBiomeProfile>();
        private int currentBiomeIndex;

        public RuntimeBiomeProfile CurrentBiome =>
            biomeSequence != null && biomeSequence.Length > 0
                ? biomeSequence[Mathf.Clamp(currentBiomeIndex, 0, biomeSequence.Length - 1)]
                : null;

        public void Configure(
            Transform playerTransform,
            RunStateManager manager,
            SpawnDirector director,
            MireGroundTiler tiler,
            MirePropScatterer scatterer,
            RuntimeBiomeProfile[] sequence)
        {
            if (runStateManager != null)
            {
                runStateManager.BossWaveCompleted -= HandleBossWaveCompleted;
            }

            player = playerTransform;
            runStateManager = manager;
            spawnDirector = director;
            groundTiler = tiler;
            propScatterer = scatterer;
            biomeSequence = sequence ?? System.Array.Empty<RuntimeBiomeProfile>();

            if (runStateManager != null && isActiveAndEnabled)
            {
                runStateManager.BossWaveCompleted += HandleBossWaveCompleted;
            }

            if (biomeSequence.Length > 0)
            {
                ApplyBiome(currentBiomeIndex, initialApplication: true);
            }
        }

        private void Start()
        {
            if (biomeSequence == null || biomeSequence.Length == 0)
            {
                biomeSequence = RuntimeBiomeProfile.CreateDefaultSequence();
                ApplyBiome(currentBiomeIndex, initialApplication: true);
            }
        }

        private void OnEnable()
        {
            if (runStateManager != null)
            {
                runStateManager.BossWaveCompleted += HandleBossWaveCompleted;
            }
        }

        private void OnDisable()
        {
            if (runStateManager != null)
            {
                runStateManager.BossWaveCompleted -= HandleBossWaveCompleted;
            }
        }

        private void HandleBossWaveCompleted()
        {
            if (biomeSequence == null || biomeSequence.Length == 0)
            {
                runStateManager?.ResumeAmbientRun(45f);
                spawnDirector?.ResumeSpawning();
                return;
            }

            currentBiomeIndex = Mathf.Min(currentBiomeIndex + 1, biomeSequence.Length - 1);
            ApplyBiome(currentBiomeIndex, initialApplication: false);
        }

        private void ApplyBiome(int biomeIndex, bool initialApplication)
        {
            if (biomeSequence == null || biomeSequence.Length == 0)
            {
                return;
            }

            var profile = biomeSequence[Mathf.Clamp(biomeIndex, 0, biomeSequence.Length - 1)];
            groundTiler?.ApplyBiomeProfile(profile);
            propScatterer?.ApplyBiomeProfile(profile);
            spawnDirector?.ConfigureBiome(profile);
            runStateManager?.ConfigureBiome(profile);

            if (initialApplication)
            {
                var initialCenter = player != null ? player.position : transform.position;
                groundTiler?.RefreshBiomeRadius(initialCenter, 1000f);
                propScatterer?.RefreshBiomeRadius(initialCenter, 1000f);
                Debug.Log($"WorldProgressionDirector initialized biome '{profile.DisplayName}'.", this);
                return;
            }

            var refreshCenter = player != null ? player.position : transform.position;
            groundTiler?.RefreshBiomeRadius(refreshCenter, localBiomeRefreshRadius);
            propScatterer?.RefreshBiomeRadius(refreshCenter, localBiomeRefreshRadius);
            spawnDirector?.TightenPressure(profile.SpawnIntervalReductionBonus, profile.AdditionalAliveCap);
            spawnDirector?.ResumeSpawning();
            runStateManager?.ResumeAmbientRun(profile.BossTransitionDelaySeconds);

            Debug.Log(
                $"WorldProgressionDirector advanced to biome '{profile.DisplayName}' at {refreshCenter} with refresh radius {localBiomeRefreshRadius:0.0}.",
                this);
        }
    }
}
