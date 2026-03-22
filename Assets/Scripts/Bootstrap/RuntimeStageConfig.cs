using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dagon.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class RuntimeStageConfig : MonoBehaviour
    {
        public enum StageKind
        {
            BlackMireRun,
            MireColossusBoss,
            DeveloperSandbox
        }

        [System.Serializable]
        public struct StageRuntimeSettings
        {
            [Min(0)] public int spawnQuota;
            [Min(0)] public int startingEnemies;
            [Min(1)] public int maxAliveEnemies;
            [Min(1)] public int eliteSpawnEvery;
            [Min(0.1f)] public float minSpawnInterval;
            [Min(0.15f)] public float maxSpawnInterval;
            public bool openingWaveEnabled;
            public bool enemyHealthBarsAlwaysVisible;
            [Min(0.1f)] public float enemyHealthBarVisibleDuration;
            [Min(1f)] public float bossTransitionDelaySeconds;
            public bool showSpawnProgressUi;
            public bool enableSandboxUi;
            public bool useSpawnRamp;
            [Min(0f)] public float spawnRampDelaySeconds;
            [Min(1f)] public float spawnRampDurationSeconds;
            [Min(0f)] public float spawnRampMaxIntervalReduction;
            [Min(0)] public int spawnRampAdditionalAliveCap;

            public StageRuntimeSettings(
                int spawnQuota,
                int startingEnemies,
                int maxAliveEnemies,
                int eliteSpawnEvery,
                float minSpawnInterval,
                float maxSpawnInterval,
                bool openingWaveEnabled,
                bool enemyHealthBarsAlwaysVisible,
                float enemyHealthBarVisibleDuration,
                float bossTransitionDelaySeconds,
                bool showSpawnProgressUi,
                bool enableSandboxUi,
                bool useSpawnRamp,
                float spawnRampDelaySeconds,
                float spawnRampDurationSeconds,
                float spawnRampMaxIntervalReduction,
                int spawnRampAdditionalAliveCap)
            {
                this.spawnQuota = spawnQuota;
                this.startingEnemies = startingEnemies;
                this.maxAliveEnemies = maxAliveEnemies;
                this.eliteSpawnEvery = eliteSpawnEvery;
                this.minSpawnInterval = minSpawnInterval;
                this.maxSpawnInterval = maxSpawnInterval;
                this.openingWaveEnabled = openingWaveEnabled;
                this.enemyHealthBarsAlwaysVisible = enemyHealthBarsAlwaysVisible;
                this.enemyHealthBarVisibleDuration = enemyHealthBarVisibleDuration;
                this.bossTransitionDelaySeconds = bossTransitionDelaySeconds;
                this.showSpawnProgressUi = showSpawnProgressUi;
                this.enableSandboxUi = enableSandboxUi;
                this.useSpawnRamp = useSpawnRamp;
                this.spawnRampDelaySeconds = spawnRampDelaySeconds;
                this.spawnRampDurationSeconds = spawnRampDurationSeconds;
                this.spawnRampMaxIntervalReduction = spawnRampMaxIntervalReduction;
                this.spawnRampAdditionalAliveCap = spawnRampAdditionalAliveCap;
            }
        }

        public readonly struct ResolvedStageConfig
        {
            public ResolvedStageConfig(StageKind stageKind, StageRuntimeSettings settings)
            {
                StageKind = stageKind;
                Settings = settings;
            }

            public StageKind StageKind { get; }
            public StageRuntimeSettings Settings { get; }
        }

        private static readonly StageRuntimeSettings BlackMireSettings = new(99999, 0, 6, 12, 1.6f, 2.3f, false, false, 2.25f, 45f, false, false, true, 10f, 45f, 1.1f, 4);
        private static readonly StageRuntimeSettings BossSettings = new(0, 0, 1, 1, 0.4f, 0.75f, false, true, 2.25f, 1f, false, false, false, 0f, 1f, 0f, 0);
        private static readonly StageRuntimeSettings DeveloperSandboxSettings = new(99999, 0, 3, 12, 2.4f, 3.6f, false, false, 2.25f, 45f, false, true, true, 20f, 90f, 1.4f, 0);

        [SerializeField] private StageKind stageKind = StageKind.BlackMireRun;

        public ResolvedStageConfig Resolve()
        {
            return stageKind switch
            {
                StageKind.DeveloperSandbox => new ResolvedStageConfig(StageKind.DeveloperSandbox, DeveloperSandboxSettings),
                StageKind.MireColossusBoss => new ResolvedStageConfig(StageKind.MireColossusBoss, BossSettings),
                _ => new ResolvedStageConfig(StageKind.BlackMireRun, BlackMireSettings)
            };
        }

        public static ResolvedStageConfig ResolveForScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return new ResolvedStageConfig(StageKind.BlackMireRun, BlackMireSettings);
            }

            if (sceneName == "MireColossusBoss")
            {
                return new ResolvedStageConfig(StageKind.MireColossusBoss, BossSettings);
            }

            if (sceneName == "DeveloperSandbox")
            {
                return new ResolvedStageConfig(StageKind.DeveloperSandbox, DeveloperSandboxSettings);
            }

            return new ResolvedStageConfig(StageKind.BlackMireRun, BlackMireSettings);
        }

        [ContextMenu("Sync Stage Kind From Scene Name")]
        private void SyncStageKindFromSceneName()
        {
            stageKind = ResolveForScene(SceneManager.GetActiveScene().name).StageKind;
        }
    }
}
