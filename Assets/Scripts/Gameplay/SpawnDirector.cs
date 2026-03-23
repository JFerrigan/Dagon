using System.Collections.Generic;
using Dagon.Bootstrap;
using Dagon.Core;
using Dagon.Rendering;
using Dagon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SpawnDirector : MonoBehaviour
    {
        private const float EnemyHurtboxHeightLeniencyMultiplier = 1.3f;

        public enum CorruptionWaveClass
        {
            Fodder,
            Specialist,
            Elite
        }

        public enum CorruptionWaveEnemyKind
        {
            Random,
            MireWretch,
            DrownedAcolyte,
            Mermaid,
            WatcherEye,
            Parasite,
            DeepSpawn
        }

        public readonly struct EnemyVisualSpec
        {
            public EnemyVisualSpec(string resourcePath, float pixelsPerUnit, Vector3 scale, Vector3 localPosition)
            {
                ResourcePath = resourcePath;
                PixelsPerUnit = pixelsPerUnit;
                Scale = scale;
                LocalPosition = localPosition;
            }

            public string ResourcePath { get; }
            public float PixelsPerUnit { get; }
            public Vector3 Scale { get; }
            public Vector3 LocalPosition { get; }
        }

        private const string DeepSpawnPrefabResourcePath = "Prefabs/Enemies/DeepSpawn";
        private const float ParasiteUnlockTime = 60f;
        private const float SpecialistHealthPickupDropChance = 0.2f;
        private const float EliteHealthPickupDropChance = 0.5f;
        private const float HealthPickupHealAmount = 2f;
        private const float SpecialistUnlockTime = 22f;
        private const float EliteUnlockTime = 58f;
        private const float MermaidUnlockTime = 46f;
        private const float WatcherEyeFodderUnlockTime = 24f;
        private const float SpecialistBudgetGainPerSecond = 0.11f;
        private const float EliteBudgetGainPerSecond = 0.045f;
        private const float SpecialistSpawnCost = 1f;
        private const float EliteSpawnCost = 1f;
        private const float SpecialistBudgetCap = 2.35f;
        private const float EliteBudgetCap = 1.6f;
        private const float SpecialistBaseCooldown = 8.5f;
        private const float SpecialistCooldownJitter = 2.25f;
        private const float EliteBaseCooldown = 19f;
        private const float EliteCooldownJitter = 5f;
        private const float WaveBaseInterval = 60f;
        private const float WaveIntervalJitter = 10f;
        private const float WaveSpawnRadiusMultiplier = 1.45f;
        private const float WaveBlockedRetryDelay = 0.25f;

        private enum SpawnClass
        {
            Fodder,
            Specialist,
            Elite
        }

        private enum WavePattern
        {
            SingleSideLine,
            DoubleSidePincer,
            SurroundRing,
            SurroundLanes
        }

        private enum EnemyKind
        {
            MireWretch,
            DrownedAcolyte,
            Mermaid,
            WatcherEye,
            Parasite,
            DeepSpawn
        }

        private sealed class ScriptedWaveState
        {
            public EnemyKind EnemyKind;
            public WavePattern Pattern;
            public Vector2 PrimaryDirection;
            public float BurstTimer;
            public int RemainingSpawns;
            public int TotalSpawns;
            public int SpawnIndex;
            public int AliveCapBonus;
        }

        [SerializeField] private Transform player;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RunStateManager runStateManager;
        [SerializeField] private CorruptionMeter corruptionMeter;
        [SerializeField] private Sprite mireSprite;
        [SerializeField] private Sprite acolyteSprite;
        [SerializeField] private Sprite mermaidSprite;
        [SerializeField] private Sprite watcherEyeSprite;
        [SerializeField] private Sprite parasiteSprite;
        [SerializeField] private Sprite deepSpawnSprite;
        [SerializeField] private GameObject deepSpawnPrefab;
        [SerializeField] private DrownedAcolyteProjectile acolyteProjectilePrefab;
        [SerializeField] private HarpoonProjectile watcherEyeProjectilePrefab;
        [SerializeField] private float spawnRadius = 14f;
        [SerializeField] private float spawnHeightOffset = 0f;
        [SerializeField] private float despawnRadius = 34f;
        [SerializeField] private float minSpawnInterval = 2.4f;
        [SerializeField] private float maxSpawnInterval = 3.6f;
        [SerializeField] private int maxAliveEnemies = 3;
        [SerializeField] private int startingEnemies = 0;
        [SerializeField] private int eliteSpawnEvery = 12;
        [SerializeField] private int regularSpawnQuota = 999;
        [SerializeField] private bool openingWaveEnabled = false;
        [SerializeField] private bool enemyHealthBarsAlwaysVisible = false;
        [SerializeField] private float enemyHealthBarVisibleDuration = 2.25f;
        [SerializeField] private bool useSpawnRamp = true;
        [SerializeField] private float spawnRampDelaySeconds = 25f;
        [SerializeField] private float spawnRampDurationSeconds = 120f;
        [SerializeField] private float spawnRampMaxIntervalReduction = 1.2f;
        [SerializeField] private int spawnRampAdditionalAliveCap;

        private float spawnTimer;
        private float configuredMinSpawnInterval;
        private float configuredMaxSpawnInterval;
        private float pressureIntervalReduction;
        private float runtimeStartedAt;
        private float specialistBudget;
        private float eliteBudget;
        private float specialistCooldownRemaining;
        private float eliteCooldownRemaining;
        private float waveTimer;
        private float activeWaveBurstTimer;
        private float bossPhaseIntervalMultiplier = 1f;
        private float corruptionFodderWaveSizeMultiplier = 1f;
        private float corruptionSpecialistWaveSizeMultiplier = 1f;
        private float corruptionEliteWaveSizeMultiplier = 1f;
        private float corruptionBossAmbientIntervalMultiplier = 1f;
        private int defeatedEnemies;
        private int totalSpawned;
        private int activeWaveRemainingSpawns;
        private int activeWaveTotalSpawns;
        private int activeWaveSpawnIndex;
        private int activeWaveAliveCapBonus;
        private int wavesStartedCount;
        private int corruptionSpecialistCapBonus;
        private int corruptionEliteCapBonus;
        private bool spawningStopped;
        private bool quotaNotified;
        private bool initialized;
        private bool waveActive;
        private bool corruptionEliteWaveUnlockOverride;
        private RuntimeBiomeProfile currentBiomeProfile;
        private bool bossPhaseSpawnThrottleActive;
        private int bossPhaseAliveCap;
        private EnemyKind activeWaveEnemyKind;
        private WavePattern activeWavePattern;
        private Vector2 activeWavePrimaryDirection;
        private readonly HashSet<GameObject> activeEnemies = new();
        private readonly List<ScriptedWaveState> scriptedWaves = new();

        private void Awake()
        {
            configuredMinSpawnInterval = minSpawnInterval;
            configuredMaxSpawnInterval = maxSpawnInterval;

            var directors = FindObjectsByType<SpawnDirector>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < directors.Length; i++)
            {
                if (directors[i] == this)
                {
                    continue;
                }

                Debug.LogWarning(
                    $"Disabling duplicate SpawnDirector on scene '{SceneManager.GetActiveScene().name}' to prevent stacked spawning.",
                    this);
                enabled = false;
                return;
            }
        }

        public event System.Action SpawnQuotaCompleted;
        public event System.Action BattlefieldCleared;

        public int AliveEnemies => activeEnemies.Count;
        public int DefeatedEnemies => defeatedEnemies;
        public int TotalSpawned => totalSpawned;
        public int RemainingSpawns => Mathf.Max(0, regularSpawnQuota - totalSpawned);
        public bool SpawnQuotaMet => totalSpawned >= regularSpawnQuota;
        public bool IsBattlefieldClear => SpawnQuotaMet && activeEnemies.Count <= 0;
        public bool EnemyHealthBarsAlwaysVisible => enemyHealthBarsAlwaysVisible;
        public bool Initialized => initialized;
        public bool OpeningWaveEnabled => openingWaveEnabled;
        public bool IsAutoSpawningStopped => spawningStopped;

        public static bool TryGetEnemyVisualSpec(string enemyId, out EnemyVisualSpec spec)
        {
            switch (enemyId)
            {
                case "mire_wretch":
                    spec = new EnemyVisualSpec("Sprites/Enemies/mire_wretch", 64f, new Vector3(5.9f, 5.9f, 1f), Vector3.zero);
                    return true;
                case "drowned_acolyte":
                    spec = new EnemyVisualSpec("Sprites/Enemies/drowned_acolyte", 256f, new Vector3(0.82f, 0.82f, 1f), Vector3.zero);
                    return true;
                case "mermaid":
                    spec = new EnemyVisualSpec("Sprites/Enemies/mermaid", 64f, new Vector3(2.2f, 2.2f, 1f), Vector3.zero);
                    return true;
                case "deep_spawn":
                    spec = new EnemyVisualSpec("Sprites/Enemies/deep_spawn", 64f, new Vector3(0.9f, 0.9f, 1f), Vector3.zero);
                    return true;
                case "watcher_eye":
                    spec = new EnemyVisualSpec("Sprites/Enemies/watcher_eye", 64f, new Vector3(1.2f, 1.2f, 1f), new Vector3(0f, 0.22f, 0f));
                    return true;
                case "parasite":
                    spec = new EnemyVisualSpec("Sprites/Enemies/parasite", 64f, new Vector3(1.25f, 1.25f, 1f), new Vector3(0f, 0.12f, 0f));
                    return true;
                case "tall_leech":
                    spec = new EnemyVisualSpec("Sprites/Enemies/tall_leech", 64f, new Vector3(1.45f, 1.45f, 1f), Vector3.zero);
                    return true;
                case "wide_leech":
                    spec = new EnemyVisualSpec("Sprites/Enemies/wide_leech", 64f, new Vector3(0.85f, 0.85f, 1f), new Vector3(0f, 0.04f, 0f));
                    return true;
                default:
                    spec = default;
                    return false;
            }
        }

        private void Start()
        {
            TryInitializeRuntime("Start");
        }

        private void Update()
        {
            if (!initialized && !TryInitializeRuntime("Update"))
            {
                return;
            }

            if (player == null || worldCamera == null)
            {
                return;
            }

            TickDirector(Time.deltaTime);
            DespawnFarEnemies();
            TickWave(Time.deltaTime);
            TickScriptedWaves(Time.deltaTime);

            if (spawningStopped || SpawnQuotaMet || activeEnemies.Count >= GetCurrentMaxAliveEnemies())
            {
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
            {
                return;
            }

            TrySpawnEncounter();
            ResetTimer();
        }

        public void Configure(Transform playerTransform, Camera cameraReference, Sprite enemySprite, HarpoonProjectile rangedProjectilePrefab)
        {
            player = playerTransform;
            worldCamera = cameraReference;
            corruptionMeter = player != null ? player.GetComponent<CorruptionMeter>() : corruptionMeter;
            if (runStateManager == null)
            {
                runStateManager = FindFirstObjectByType<RunStateManager>();
            }
            mireSprite = enemySprite;
            acolyteSprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Enemies/drowned_acolyte", 256f);
            mermaidSprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Enemies/mermaid", 64f);
            watcherEyeSprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Enemies/watcher_eye", 64f);
            parasiteSprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Enemies/parasite", 64f);
            deepSpawnSprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Enemies/deep_spawn", 64f);
            deepSpawnPrefab = Resources.Load<GameObject>(DeepSpawnPrefabResourcePath);
            acolyteProjectilePrefab = RuntimeAcolyteProjectileFactory.Create(cameraReference);
            watcherEyeProjectilePrefab = RuntimeOrbProjectileFactory.Create(
                cameraReference,
                "Sprites/Enemies/watcher_eye",
                new Color(0.84f, 0.96f, 0.82f, 1f),
                new Vector3(0.18f, 0.18f, 1f),
                64f);
        }

        public void ConfigureCampaign(int newRegularSpawnQuota, int newStartingEnemies, int newMaxAliveEnemies, int newEliteSpawnEvery, float newMinSpawnInterval, float newMaxSpawnInterval)
        {
            regularSpawnQuota = Mathf.Max(0, newRegularSpawnQuota);
            startingEnemies = Mathf.Max(0, newStartingEnemies);
            maxAliveEnemies = Mathf.Max(1, newMaxAliveEnemies);
            eliteSpawnEvery = Mathf.Max(1, newEliteSpawnEvery);
            minSpawnInterval = Mathf.Max(0.1f, newMinSpawnInterval);
            maxSpawnInterval = Mathf.Max(minSpawnInterval + 0.05f, newMaxSpawnInterval);
            configuredMinSpawnInterval = minSpawnInterval;
            configuredMaxSpawnInterval = maxSpawnInterval;
            pressureIntervalReduction = 0f;
            ResetDirectorState();
            spawningStopped = false;
            quotaNotified = false;
        }

        public void ConfigureHealthBars(bool alwaysVisible, float visibleDurationAfterDamage = 2.25f)
        {
            enemyHealthBarsAlwaysVisible = alwaysVisible;
            enemyHealthBarVisibleDuration = Mathf.Max(0.1f, visibleDurationAfterDamage);
        }

        public void ConfigureSpawnFlow(bool enableOpeningWave)
        {
            openingWaveEnabled = enableOpeningWave;
        }

        public void ConfigureSpawnRamp(bool enabled, float delaySeconds, float durationSeconds, float maxIntervalReduction, int additionalAliveCap = 0)
        {
            useSpawnRamp = enabled;
            spawnRampDelaySeconds = Mathf.Max(0f, delaySeconds);
            spawnRampDurationSeconds = Mathf.Max(1f, durationSeconds);
            spawnRampMaxIntervalReduction = Mathf.Max(0f, maxIntervalReduction);
            spawnRampAdditionalAliveCap = Mathf.Max(0, additionalAliveCap);
        }

        public bool TriggerCorruptionWave(CorruptionWaveClass waveClass)
        {
            var elapsed = GetElapsedRunTime();
            EnemyKind enemyKind;
            switch (waveClass)
            {
                case CorruptionWaveClass.Specialist:
                    if (!HasAvailableSpecialist(elapsed))
                    {
                        return false;
                    }

                    enemyKind = ChooseWaveSpecialistKind(elapsed);
                    break;
                case CorruptionWaveClass.Elite:
                    if (!HasAvailableElite())
                    {
                        return false;
                    }

                    enemyKind = ChooseWaveEliteKind();
                    break;
                default:
                    enemyKind = ChooseWaveFodderKind(elapsed);
                    break;
            }

            return TriggerCorruptionWave(enemyKind);
        }

        public bool TriggerCorruptionWave(CorruptionWaveEnemyKind enemyKind)
        {
            return TriggerCorruptionWave(enemyKind switch
            {
                CorruptionWaveEnemyKind.MireWretch => EnemyKind.MireWretch,
                CorruptionWaveEnemyKind.DrownedAcolyte => EnemyKind.DrownedAcolyte,
                CorruptionWaveEnemyKind.Mermaid => EnemyKind.Mermaid,
                CorruptionWaveEnemyKind.WatcherEye => EnemyKind.WatcherEye,
                CorruptionWaveEnemyKind.Parasite => EnemyKind.Parasite,
                CorruptionWaveEnemyKind.DeepSpawn => EnemyKind.DeepSpawn,
                _ => ChooseWaveFodderKind(GetElapsedRunTime())
            });
        }

        public bool InitializeRuntime(string contextLabel = "Explicit")
        {
            return TryInitializeRuntime(contextLabel, emitWarnings: true);
        }

        public void StopSpawning()
        {
            spawningStopped = true;
        }

        public void ResumeSpawning()
        {
            if (SpawnQuotaMet)
            {
                return;
            }

            spawningStopped = false;
            bossPhaseSpawnThrottleActive = false;
            bossPhaseIntervalMultiplier = 1f;
            bossPhaseAliveCap = 0;
            ResetDirectorCooldowns();
            ResetTimer();
        }

        public void EnterBossAmbientPressure(float intervalMultiplier, int aliveCapClamp)
        {
            if (SpawnQuotaMet)
            {
                return;
            }

            bossPhaseSpawnThrottleActive = true;
            bossPhaseIntervalMultiplier = Mathf.Max(1f, intervalMultiplier);
            bossPhaseAliveCap = Mathf.Max(0, aliveCapClamp);
            spawningStopped = false;
            ResetTimer();
        }

        public void ConfigureStage(int startingEnemyCount, bool disableContinuousSpawning)
        {
            startingEnemies = Mathf.Max(0, startingEnemyCount);
            if (disableContinuousSpawning)
            {
                spawningStopped = true;
            }
        }

        public void TightenPressure(float intervalReduction, int additionalAliveCap)
        {
            pressureIntervalReduction = Mathf.Max(0f, pressureIntervalReduction + intervalReduction);
            maxAliveEnemies = Mathf.Max(1, maxAliveEnemies + additionalAliveCap);
        }

        public void ConfigureCorruptionModifiers(
            float fodderWaveSizeMultiplier,
            float specialistWaveSizeMultiplier,
            float eliteWaveSizeMultiplier,
            int specialistCapBonus,
            int eliteCapBonus,
            bool eliteWaveUnlockOverride,
            float bossAmbientIntervalMultiplier)
        {
            corruptionFodderWaveSizeMultiplier = Mathf.Max(0.1f, fodderWaveSizeMultiplier);
            corruptionSpecialistWaveSizeMultiplier = Mathf.Max(0.1f, specialistWaveSizeMultiplier);
            corruptionEliteWaveSizeMultiplier = Mathf.Max(0.1f, eliteWaveSizeMultiplier);
            corruptionSpecialistCapBonus = Mathf.Max(0, specialistCapBonus);
            corruptionEliteCapBonus = Mathf.Max(0, eliteCapBonus);
            corruptionEliteWaveUnlockOverride = eliteWaveUnlockOverride;
            corruptionBossAmbientIntervalMultiplier = Mathf.Max(1f, bossAmbientIntervalMultiplier);
        }

        public void IncreaseSandboxPressure(float intervalReduction = 0.18f, int additionalAliveCap = 1)
        {
            TightenPressure(intervalReduction, additionalAliveCap);
            spawnTimer = Mathf.Min(spawnTimer, GetCurrentMinSpawnInterval());
        }

        public bool SpawnSandboxMireWretch()
        {
            return SpawnSandboxMireWretch(false);
        }

        public bool SpawnSandboxDrownedAcolyte()
        {
            return SpawnSandboxDrownedAcolyte(false);
        }

        public bool SpawnSandboxMermaid()
        {
            return SpawnSandboxMermaid(false);
        }

        public bool SpawnSandboxWatcherEye()
        {
            return SpawnSandboxWatcherEye(false);
        }

        public bool SpawnSandboxParasite()
        {
            return SpawnSandboxParasite(false);
        }

        public bool SpawnSandboxDeepSpawn()
        {
            return SpawnSandboxDeepSpawn(false);
        }

        public bool SpawnSandboxMireWretch(bool forceCorrupted)
        {
            return TrySpawnSpecificEnemy(EnemyKind.MireWretch, BuildSpawnPosition(), ignoreAliveCap: true, forceCorrupted: forceCorrupted);
        }

        public bool SpawnSandboxDrownedAcolyte(bool forceCorrupted)
        {
            return TrySpawnSpecificEnemy(EnemyKind.DrownedAcolyte, BuildSpawnPosition(), ignoreAliveCap: true, forceCorrupted: forceCorrupted);
        }

        public bool SpawnSandboxMermaid(bool forceCorrupted)
        {
            return TrySpawnSpecificEnemy(EnemyKind.Mermaid, BuildSpawnPosition(), ignoreAliveCap: true, forceCorrupted: forceCorrupted);
        }

        public bool SpawnSandboxWatcherEye(bool forceCorrupted)
        {
            return TrySpawnSpecificEnemy(EnemyKind.WatcherEye, BuildSpawnPosition(), ignoreAliveCap: true, forceCorrupted: forceCorrupted);
        }

        public bool SpawnSandboxParasite(bool forceCorrupted)
        {
            return TrySpawnSpecificEnemy(EnemyKind.Parasite, BuildSpawnPosition(), ignoreAliveCap: true, forceCorrupted: forceCorrupted);
        }

        public bool SpawnSandboxDeepSpawn(bool forceCorrupted)
        {
            return TrySpawnSpecificEnemy(EnemyKind.DeepSpawn, BuildSpawnPosition(), ignoreAliveCap: true, forceCorrupted: forceCorrupted);
        }

        public void ConfigureBiome(RuntimeBiomeProfile profile)
        {
            currentBiomeProfile = profile;
            mireSprite = LoadBiomeSprite(profile != null ? profile.MireWretchSpritePath : null, "Sprites/Enemies/mire_wretch", mireSprite);
            acolyteSprite = LoadBiomeSprite(profile != null ? profile.DrownedAcolyteSpritePath : null, "Sprites/Enemies/drowned_acolyte", acolyteSprite);
            mermaidSprite = LoadBiomeSprite(profile != null ? profile.MermaidSpritePath : null, "Sprites/Enemies/mermaid", mermaidSprite, 64f);
            watcherEyeSprite = LoadBiomeSprite(profile != null ? profile.WatcherEyeSpritePath : null, "Sprites/Enemies/watcher_eye", watcherEyeSprite, 64f);
            parasiteSprite = LoadBiomeSprite(profile != null ? profile.ParasiteSpritePath : null, "Sprites/Enemies/parasite", parasiteSprite, 64f);
            deepSpawnSprite = LoadBiomeSprite(profile != null ? profile.DeepSpawnSpritePath : null, "Sprites/Enemies/deep_spawn", deepSpawnSprite, 64f);
        }

        private bool TryInitializeRuntime(string contextLabel, bool emitWarnings = false)
        {
            if (initialized)
            {
                return true;
            }

            if (player == null || worldCamera == null || mireSprite == null)
            {
                if (emitWarnings)
                {
                    Debug.LogWarning(
                        $"SpawnDirector on scene '{SceneManager.GetActiveScene().name}' could not initialize from {contextLabel}. " +
                        $"Player assigned: {player != null}, camera assigned: {worldCamera != null}, mire sprite assigned: {mireSprite != null}.",
                        this);
                }

                return false;
            }

            initialized = true;
            runtimeStartedAt = Time.time;
            if (runStateManager == null)
            {
                runStateManager = FindFirstObjectByType<RunStateManager>();
            }
            if (corruptionMeter == null && player != null)
            {
                corruptionMeter = player.GetComponent<CorruptionMeter>();
            }
            ResetDirectorState();
            if (openingWaveEnabled)
            {
                SpawnOpeningWave();
            }

            ResetTimer();
            Debug.Log(
                $"SpawnDirector initialized for scene '{SceneManager.GetActiveScene().name}' via {contextLabel}. " +
                $"Quota={regularSpawnQuota}, Starting={startingEnemies}, MaxAlive={maxAliveEnemies}, " +
                $"EliteEvery={eliteSpawnEvery}, Interval={minSpawnInterval:0.00}-{maxSpawnInterval:0.00}, OpeningWaveEnabled={openingWaveEnabled}, " +
                $"BarsAlwaysVisible={enemyHealthBarsAlwaysVisible}, BarVisibleDuration={enemyHealthBarVisibleDuration:0.00}, " +
                $"SpawnRamp={useSpawnRamp}, RampDelay={spawnRampDelaySeconds:0.0}, RampDuration={spawnRampDurationSeconds:0.0}, " +
                $"RampMaxReduction={spawnRampMaxIntervalReduction:0.00}, RampAliveCap={spawnRampAdditionalAliveCap}.",
                this);
            return true;
        }

        private void SpawnOpeningWave()
        {
            var count = Mathf.Min(startingEnemies, Mathf.Min(regularSpawnQuota - totalSpawned, GetCurrentMaxAliveEnemies() - activeEnemies.Count));
            for (var i = 0; i < count; i++)
            {
                if (!TrySpawnSpecificEnemy(EnemyKind.MireWretch, BuildSpawnPosition()))
                {
                    break;
                }
            }
        }

        private bool TrySpawnEncounter()
        {
            if (SpawnQuotaMet || totalSpawned >= regularSpawnQuota || activeEnemies.Count >= GetCurrentMaxAliveEnemies())
            {
                NotifyQuotaCompletedIfNeeded();
                return false;
            }

            var nextEnemyKind = ChooseNextEnemyKind();
            var spawnedAny = nextEnemyKind == EnemyKind.Parasite
                ? TrySpawnParasitePack()
                : TrySpawnSpecificEnemy(nextEnemyKind, BuildSpawnPosition());
            if (SpawnQuotaMet)
            {
                NotifyQuotaCompletedIfNeeded();
            }

            return spawnedAny;
        }

        private bool TrySpawnSpecificEnemy(EnemyKind enemyKind, Vector3 position, bool ignoreAliveCap = false, bool forceCorrupted = false)
        {
            if (player == null || mireSprite == null || SpawnQuotaMet || totalSpawned >= regularSpawnQuota)
            {
                return false;
            }

            if (!ignoreAliveCap && activeEnemies.Count >= GetCurrentMaxAliveEnemies())
            {
                return false;
            }

            var isCorrupted = forceCorrupted || ShouldSpawnCorruptedEnemy(enemyKind);
            if (enemyKind == EnemyKind.DeepSpawn && TrySpawnDeepSpawn(position, isCorrupted))
            {
                return true;
            }

            var mire = new GameObject($"{(isCorrupted ? "Corrupted" : string.Empty)}{enemyKind}_{activeEnemies.Count + 1}");
            mire.transform.SetParent(transform);
            mire.transform.position = position;
            var modifiers = isCorrupted
                ? CorruptionVariantRules.GetEnemyModifiers(GetEnemyArchetype(enemyKind))
                : new CorruptionVariantRules.StatModifiers(1f, 1f, 1f, 1f);

            var collider = mire.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            ConfigureCollider(collider, enemyKind);
            ConfigureBodyBlocker(mire, enemyKind, collider);

            var rigidbody = mire.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            var health = mire.AddComponent<Health>();
            health.SetMaxHealth(GetMaxHealth(enemyKind) * modifiers.HealthMultiplier, true);
            health.Died += HandleEnemyDied;
            mire.AddComponent<Hurtbox>().Configure(CombatTeam.Enemy, health);
            var knockbackReceiver = mire.AddComponent<KnockbackReceiver>();

            var rewards = mire.AddComponent<EnemyDeathRewards>();
            switch (enemyKind)
            {
                case EnemyKind.MireWretch:
                {
                    knockbackReceiver.Configure(1f, 18f, 5.5f);
                    var contactDamage = mire.AddComponent<ContactDamage>();
                    contactDamage.Configure(1f * modifiers.DamageMultiplier);

                    var wanderer = mire.AddComponent<MireWanderer>();
                    wanderer.Configure(player, Random.Range(3.2f, 3.6f) * modifiers.SpeedMultiplier, 3f, 18f);
                    rewards.Configure(1, 0f);
                    break;
                }
                case EnemyKind.DrownedAcolyte:
                {
                    knockbackReceiver.Configure(0.85f, 18f, 5f);
                    var shooter = mire.AddComponent<DrownedAcolyteShooter>();
                    shooter.Configure(player, acolyteProjectilePrefab, Random.Range(2.4f, 2.8f), 6f, 1.6f, worldCamera);
                    if (isCorrupted)
                    {
                        shooter.ApplyCorruptionModifiers(modifiers.DamageMultiplier, modifiers.SpeedMultiplier, modifiers.CadenceMultiplier);
                    }
                    rewards.Configure(3, 2f, SpecialistHealthPickupDropChance, HealthPickupHealAmount);
                    break;
                }
                case EnemyKind.Mermaid:
                {
                    knockbackReceiver.Configure(0.8f, 18f, 5f);
                    var mermaid = mire.AddComponent<MermaidController>();
                    mermaid.Configure(player, worldCamera, Random.Range(2.1f, 2.4f), 7.1f);
                    if (isCorrupted)
                    {
                        mermaid.ApplyCorruptionModifiers(modifiers.DamageMultiplier, modifiers.SpeedMultiplier, modifiers.CadenceMultiplier);
                    }
                    rewards.Configure(4, 2f, SpecialistHealthPickupDropChance, HealthPickupHealAmount);
                    break;
                }
                case EnemyKind.WatcherEye:
                {
                    knockbackReceiver.Configure(0.7f, 18f, 5f);
                    var watcherEye = mire.AddComponent<WatcherEyeController>();
                    watcherEye.Configure(player, watcherEyeProjectilePrefab, worldCamera, Random.Range(2.2f, 2.5f), 7.8f);
                    if (isCorrupted)
                    {
                        watcherEye.ApplyCorruptionModifiers(modifiers.DamageMultiplier, modifiers.SpeedMultiplier, modifiers.CadenceMultiplier);
                    }
                    rewards.Configure(1, 2f, SpecialistHealthPickupDropChance, HealthPickupHealAmount);
                    break;
                }
                case EnemyKind.Parasite:
                {
                    knockbackReceiver.Configure(1.2f, 16f, 5.8f);
                    var contactDamage = mire.AddComponent<ContactDamage>();
                    contactDamage.Configure(1f * modifiers.DamageMultiplier);
                    var parasite = mire.AddComponent<ParasiteChaser>();
                    parasite.Configure(player, Random.Range(7.5f, 8f), 0.2f);
                    if (isCorrupted)
                    {
                        parasite.ApplyCorruptionModifiers(modifiers.SpeedMultiplier);
                    }
                    rewards.Configure(1, 0f);
                    break;
                }
                case EnemyKind.DeepSpawn:
                default:
                {
                    knockbackReceiver.Configure(0.45f, 20f, 4.5f);
                    var bruiser = mire.AddComponent<DeepSpawnBruiser>();
                    bruiser.Configure(player, 1.2f, 4.8f);
                    var contactDamage = mire.AddComponent<ContactDamage>();
                    contactDamage.Configure(3f);
                    rewards.Configure(6, 4f, EliteHealthPickupDropChance, HealthPickupHealAmount);
                    break;
                }
            }

            ConfigureVisuals(mire.transform, enemyKind, isCorrupted);
            ConfigureHealthBar(mire.transform, health, enemyKind);
            RegisterEnemy(mire);
            totalSpawned += 1;
            return true;
        }

        private bool TrySpawnParasitePack()
        {
            if (player == null)
            {
                return false;
            }

            var availableSlots = Mathf.Min(
                regularSpawnQuota - totalSpawned,
                GetCurrentMaxAliveEnemies() - activeEnemies.Count);
            var maxPackSize = Mathf.Clamp(availableSlots, 1, 4);
            var packSize = Random.Range(1, maxPackSize + 1);
            var anchor = BuildSpawnPosition();
            var toPlayer = player.position - anchor;
            toPlayer.y = 0f;
            var forward = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector3.forward;
            var lateral = new Vector3(-forward.z, 0f, forward.x);
            var spacing = 1.15f;
            var halfWidth = (packSize - 1) * 0.5f;
            var spawnedAny = false;

            for (var index = 0; index < packSize; index++)
            {
                var lateralOffset = (index - halfWidth) * spacing;
                var position = anchor + (lateral * lateralOffset);
                position.y = player.position.y + spawnHeightOffset;
                if (TrySpawnSpecificEnemy(EnemyKind.Parasite, position))
                {
                    spawnedAny = true;
                }
            }

            return spawnedAny;
        }

        private void ConfigureVisuals(Transform enemyRoot, EnemyKind enemyKind, bool isCorrupted)
        {
            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(enemyRoot, false);
            visuals.transform.localPosition = Vector3.zero;

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSprite(enemyKind);
            renderer.sortingOrder = enemyKind == EnemyKind.DeepSpawn ? 7 : enemyKind == EnemyKind.MireWretch || enemyKind == EnemyKind.Parasite ? 5 : 6;
            var baseColor = currentBiomeProfile != null ? currentBiomeProfile.EnemyTint : Color.white;
            renderer.color = baseColor;

            if (enemyKind == EnemyKind.MireWretch)
            {
                visuals.transform.localScale = new Vector3(5.9f, 5.9f, 1f);
            }
            else if (enemyKind == EnemyKind.DeepSpawn)
            {
                visuals.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            }
            else if (enemyKind == EnemyKind.Mermaid && mermaidSprite != null)
            {
                visuals.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
            }
            else if (enemyKind == EnemyKind.WatcherEye && watcherEyeSprite != null)
            {
                visuals.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                visuals.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            }
            else if (enemyKind == EnemyKind.Parasite && parasiteSprite != null)
            {
                visuals.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
                visuals.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            }
            else if (enemyKind == EnemyKind.DrownedAcolyte && acolyteSprite != null)
            {
                visuals.transform.localScale = new Vector3(0.82f, 0.82f, 1f);
            }

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);

            if (isCorrupted)
            {
                visuals.AddComponent<CorruptedVariantVisual>().Apply(renderer, baseColor);
            }
        }

        private bool TrySpawnDeepSpawn(Vector3 position, bool isCorrupted)
        {
            if (deepSpawnPrefab == null)
            {
                return false;
            }

            var deepSpawnObject = Instantiate(deepSpawnPrefab, position, Quaternion.identity, transform);
            deepSpawnObject.name = $"{(isCorrupted ? "Corrupted" : string.Empty)}{EnemyKind.DeepSpawn}_{activeEnemies.Count + 1}";

            var deepSpawn = deepSpawnObject.GetComponent<DeepSpawnPrefab>();
            if (deepSpawn == null)
            {
                Destroy(deepSpawnObject);
                return false;
            }

            deepSpawn.Configure(player, worldCamera, enemyHealthBarsAlwaysVisible, enemyHealthBarVisibleDuration);
            if (isCorrupted)
            {
                var modifiers = CorruptionVariantRules.GetEnemyModifiers(CorruptionVariantRules.EnemyArchetype.Elite);
                deepSpawn.ApplyCorruptionModifiers(
                    modifiers.HealthMultiplier,
                    modifiers.DamageMultiplier,
                    modifiers.SpeedMultiplier,
                    modifiers.CadenceMultiplier);
                var visuals = deepSpawnObject.transform.Find("Visuals");
                var renderer = visuals != null ? visuals.GetComponent<SpriteRenderer>() : null;
                if (renderer != null)
                {
                    var baseColor = currentBiomeProfile != null ? currentBiomeProfile.EnemyTint : Color.white;
                    renderer.gameObject.AddComponent<CorruptedVariantVisual>().Apply(renderer, baseColor);
                }
            }

            var health = deepSpawn.HealthComponent;
            if (health == null)
            {
                Destroy(deepSpawnObject);
                return false;
            }

            health.Died += HandleEnemyDied;
            ConfigureHealthBar(deepSpawnObject.transform, health, EnemyKind.DeepSpawn);
            RegisterEnemy(deepSpawnObject);
            totalSpawned += 1;
            return true;
        }

        private bool ShouldSpawnCorruptedEnemy(EnemyKind enemyKind)
        {
            if (corruptionMeter == null && player != null)
            {
                corruptionMeter = player.GetComponent<CorruptionMeter>();
            }

            if (corruptionMeter == null)
            {
                return false;
            }

            return Random.value <= CorruptionVariantRules.GetEnemyCorruptionChance(corruptionMeter.CurrentCorruption);
        }

        private static CorruptionVariantRules.EnemyArchetype GetEnemyArchetype(EnemyKind enemyKind)
        {
            return enemyKind switch
            {
                EnemyKind.DrownedAcolyte => CorruptionVariantRules.EnemyArchetype.Specialist,
                EnemyKind.Mermaid => CorruptionVariantRules.EnemyArchetype.Specialist,
                EnemyKind.DeepSpawn => CorruptionVariantRules.EnemyArchetype.Elite,
                _ => CorruptionVariantRules.EnemyArchetype.Fodder
            };
        }

        private void ConfigureHealthBar(Transform enemyRoot, Health health, EnemyKind enemyKind)
        {
            if (enemyRoot == null || health == null)
            {
                return;
            }

            var bar = enemyRoot.GetComponent<EnemyHealthBar>() ?? enemyRoot.gameObject.AddComponent<EnemyHealthBar>();
            var offset = enemyKind switch
            {
                EnemyKind.MireWretch => new Vector3(0f, 4.2f, 0f),
                EnemyKind.DrownedAcolyte => new Vector3(0f, 1.4f, 0f),
                EnemyKind.Mermaid => new Vector3(0f, 2.6f, 0f),
                EnemyKind.WatcherEye => new Vector3(0f, 1.7f, 0f),
                EnemyKind.Parasite => new Vector3(0f, 1.1f, 0f),
                _ => new Vector3(0f, 1.8f, 0f)
            };
            bar.Configure(worldCamera, offset, !enemyHealthBarsAlwaysVisible, enemyHealthBarVisibleDuration);
        }

        private static void ConfigureCollider(CapsuleCollider collider, EnemyKind enemyKind)
        {
            if (collider == null)
            {
                return;
            }

            switch (enemyKind)
            {
                case EnemyKind.MireWretch:
                    collider.center = new Vector3(0f, 1.8f, 0f);
                    collider.height = 3.6f;
                    collider.radius = 1.1f;
                    break;
                case EnemyKind.Mermaid:
                    collider.center = new Vector3(0f, 1.15f, 0f);
                    collider.height = 2.3f;
                    collider.radius = 0.65f;
                    break;
                case EnemyKind.WatcherEye:
                    collider.center = new Vector3(0f, 0.72f, 0f);
                    collider.height = 1.45f;
                    collider.radius = 0.42f;
                    break;
                case EnemyKind.Parasite:
                    collider.center = new Vector3(0f, 0.45f, 0f);
                    collider.height = 0.9f;
                    collider.radius = 0.38f;
                    break;
                case EnemyKind.DeepSpawn:
                    collider.center = new Vector3(0f, 0.8f, 0f);
                    collider.height = 1.6f;
                    collider.radius = 0.5f;
                    break;
                default:
                    collider.center = new Vector3(0f, 0.75f, 0f);
                    collider.height = 1.5f;
                    collider.radius = 0.35f;
                    break;
            }

            ApplyVerticalHurtboxLeniency(collider);
        }

        private static void ApplyVerticalHurtboxLeniency(CapsuleCollider collider)
        {
            var originalHeight = Mathf.Max(collider.radius * 2f, collider.height);
            var expandedHeight = Mathf.Max(originalHeight, originalHeight * EnemyHurtboxHeightLeniencyMultiplier);
            var extraHeight = expandedHeight - originalHeight;
            collider.height = expandedHeight;
            collider.center += new Vector3(0f, extraHeight * 0.5f, 0f);
        }

        private static void ConfigureBodyBlocker(GameObject enemyRoot, EnemyKind enemyKind, CapsuleCollider collider)
        {
            if (enemyRoot == null || collider == null)
            {
                return;
            }

            var blocker = enemyRoot.GetComponent<BodyBlocker>() ?? enemyRoot.AddComponent<BodyBlocker>();
            var radius = enemyKind switch
            {
                EnemyKind.MireWretch => 0.9f,
                EnemyKind.DrownedAcolyte => 0.42f,
                EnemyKind.Mermaid => 0.58f,
                EnemyKind.WatcherEye => 0.38f,
                EnemyKind.Parasite => 0.28f,
                EnemyKind.DeepSpawn => 0.62f,
                _ => Mathf.Max(0.2f, collider.radius * 0.85f)
            };
            var weight = enemyKind switch
            {
                EnemyKind.Parasite => 0.7f,
                EnemyKind.WatcherEye => 0.85f,
                EnemyKind.DeepSpawn => 1.8f,
                EnemyKind.Mermaid => 1.15f,
                _ => 1f
            };
            blocker.Configure(BodyBlocker.BodyTeam.Enemy, radius, collider.height, weight, true, true);
        }

        private void HandleEnemyDied(Health health, GameObject source)
        {
            health.Died -= HandleEnemyDied;
            UnregisterEnemy(health != null ? health.gameObject : null);
            defeatedEnemies = Mathf.Min(regularSpawnQuota, defeatedEnemies + 1);
            if (IsBattlefieldClear)
            {
                BattlefieldCleared?.Invoke();
                return;
            }

            if (SpawnQuotaMet)
            {
                NotifyQuotaCompletedIfNeeded();
            }
        }

        private void ResetTimer()
        {
            var currentMinInterval = GetCurrentMinSpawnInterval();
            var currentMaxInterval = GetCurrentMaxSpawnInterval(currentMinInterval);
            spawnTimer = Random.Range(currentMinInterval, currentMaxInterval) * bossPhaseIntervalMultiplier * corruptionBossAmbientIntervalMultiplier;
        }

        private void TickDirector(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            specialistCooldownRemaining = Mathf.Max(0f, specialistCooldownRemaining - deltaTime);
            eliteCooldownRemaining = Mathf.Max(0f, eliteCooldownRemaining - deltaTime);

            var elapsed = GetElapsedRunTime();
            if (elapsed >= SpecialistUnlockTime)
            {
                specialistBudget = Mathf.Min(
                    SpecialistBudgetCap,
                    specialistBudget + (SpecialistBudgetGainPerSecond * deltaTime));
            }

            if (elapsed >= EliteUnlockTime)
            {
                eliteBudget = Mathf.Min(
                    EliteBudgetCap,
                    eliteBudget + (EliteBudgetGainPerSecond * deltaTime));
            }
        }

        private void TickWave(float deltaTime)
        {
            if (deltaTime <= 0f || spawningStopped || SpawnQuotaMet)
            {
                return;
            }

            if (runStateManager != null && runStateManager.BossWaveStarted)
            {
                return;
            }

            if (!waveActive)
            {
                waveTimer -= deltaTime;
                if (waveTimer <= 0f)
                {
                    TryStartWave();
                }

                return;
            }

            activeWaveBurstTimer -= deltaTime;
            if (activeWaveBurstTimer > 0f)
            {
                return;
            }

            if (activeWaveRemainingSpawns <= 0)
            {
                EndWave();
                return;
            }

            if (TrySpawnWaveEnemy())
            {
                activeWaveRemainingSpawns -= 1;
                activeWaveSpawnIndex += 1;
                activeWaveBurstTimer = GetWaveBurstInterval(activeWaveEnemyKind);
                if (activeWaveRemainingSpawns <= 0)
                {
                    EndWave();
                }

                return;
            }

            if (SpawnQuotaMet || totalSpawned >= regularSpawnQuota)
            {
                EndWave();
                return;
            }

            activeWaveBurstTimer = WaveBlockedRetryDelay;
        }

        private void TickScriptedWaves(float deltaTime)
        {
            if (deltaTime <= 0f || scriptedWaves.Count <= 0)
            {
                return;
            }

            for (var i = scriptedWaves.Count - 1; i >= 0; i--)
            {
                var wave = scriptedWaves[i];
                wave.BurstTimer -= deltaTime;
                if (wave.BurstTimer > 0f)
                {
                    continue;
                }

                if (wave.RemainingSpawns <= 0)
                {
                    scriptedWaves.RemoveAt(i);
                    continue;
                }

                if (TrySpawnScriptedWaveEnemy(wave))
                {
                    wave.RemainingSpawns -= 1;
                    wave.SpawnIndex += 1;
                    wave.BurstTimer = GetWaveBurstInterval(wave.EnemyKind);
                    if (wave.RemainingSpawns <= 0)
                    {
                        scriptedWaves.RemoveAt(i);
                    }

                    continue;
                }

                wave.BurstTimer = WaveBlockedRetryDelay;
                if (SpawnQuotaMet || totalSpawned >= regularSpawnQuota)
                {
                    scriptedWaves.RemoveAt(i);
                }
            }
        }

        private float GetCurrentMinSpawnInterval()
        {
            return Mathf.Max(0.15f, configuredMinSpawnInterval - GetCurrentIntervalReduction());
        }

        private float GetCurrentMaxSpawnInterval(float currentMinInterval)
        {
            return Mathf.Max(currentMinInterval + 0.05f, configuredMaxSpawnInterval - GetCurrentIntervalReduction());
        }

        private float GetCurrentIntervalReduction()
        {
            return pressureIntervalReduction + GetSpawnRampReduction();
        }

        private float GetSpawnRampReduction()
        {
            if (!useSpawnRamp || !initialized || spawnRampMaxIntervalReduction <= 0f)
            {
                return 0f;
            }

            var elapsed = Time.time - runtimeStartedAt - spawnRampDelaySeconds;
            if (elapsed <= 0f)
            {
                return 0f;
            }

            var progress = Mathf.Clamp01(elapsed / spawnRampDurationSeconds);
            return spawnRampMaxIntervalReduction * progress;
        }

        private int GetCurrentMaxAliveEnemies()
        {
            var effectiveAliveCap = maxAliveEnemies + GetSpawnRampAliveCapBonus();
            if (waveActive)
            {
                effectiveAliveCap += activeWaveAliveCapBonus;
            }

            for (var i = 0; i < scriptedWaves.Count; i++)
            {
                effectiveAliveCap += scriptedWaves[i].AliveCapBonus;
            }

            if (bossPhaseSpawnThrottleActive && bossPhaseAliveCap > 0)
            {
                effectiveAliveCap = Mathf.Min(effectiveAliveCap, bossPhaseAliveCap);
            }

            return Mathf.Max(1, effectiveAliveCap);
        }

        private int GetSpawnRampAliveCapBonus()
        {
            if (!useSpawnRamp || !initialized || spawnRampAdditionalAliveCap <= 0)
            {
                return 0;
            }

            var elapsed = Time.time - runtimeStartedAt - spawnRampDelaySeconds;
            if (elapsed <= 0f)
            {
                return 0;
            }

            var progress = Mathf.Clamp01(elapsed / spawnRampDurationSeconds);
            return Mathf.RoundToInt(spawnRampAdditionalAliveCap * progress);
        }

        private void NotifyQuotaCompletedIfNeeded()
        {
            if (quotaNotified)
            {
                return;
            }

            quotaNotified = true;
            SpawnQuotaCompleted?.Invoke();
        }

        private void DespawnFarEnemies()
        {
            activeEnemies.RemoveWhere(enemy => enemy == null);

            if (player == null || activeEnemies.Count <= 0)
            {
                return;
            }

            var despawnRadiusSquared = despawnRadius * despawnRadius;
            var staleEnemies = ListPool<GameObject>.Get();
            foreach (var enemy in activeEnemies)
            {
                var offset = enemy.transform.position - player.position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= despawnRadiusSquared)
                {
                    continue;
                }

                staleEnemies.Add(enemy);
            }

            for (var i = 0; i < staleEnemies.Count; i++)
            {
                DespawnEnemy(staleEnemies[i]);
            }

            ListPool<GameObject>.Release(staleEnemies);
        }

        private void DespawnEnemy(GameObject enemy)
        {
            if (enemy == null)
            {
                return;
            }

            UnregisterEnemy(enemy);

            var health = enemy.GetComponent<Health>();
            if (health != null)
            {
                health.Died -= HandleEnemyDied;
            }

            Destroy(enemy);
        }

        private EnemyKind ChooseNextEnemyKind()
        {
            return ChooseSpawnClass() switch
            {
                SpawnClass.Elite => ChooseEliteKind(),
                SpawnClass.Specialist => ChooseSpecialistKind(),
                _ => ChooseFodderKind()
            };
        }

        private SpawnClass ChooseSpawnClass()
        {
            var elapsed = GetElapsedRunTime();
            var aliveSpecialists = GetAliveCountForClass(SpawnClass.Specialist);
            var aliveElites = GetAliveCountForClass(SpawnClass.Elite);

            var specialistReady =
                elapsed >= SpecialistUnlockTime &&
                HasAvailableSpecialist(elapsed) &&
                specialistBudget >= SpecialistSpawnCost &&
                specialistCooldownRemaining <= 0f &&
                aliveSpecialists < GetCurrentSpecialistCap(elapsed);

            var eliteReady =
                elapsed >= EliteUnlockTime &&
                HasAvailableElite() &&
                eliteBudget >= EliteSpawnCost &&
                eliteCooldownRemaining <= 0f &&
                aliveElites < GetCurrentEliteCap(elapsed);

            if (!specialistReady && !eliteReady)
            {
                return SpawnClass.Fodder;
            }

            var specialistWeight = specialistReady ? GetSpecialistDirectorWeight(elapsed) : 0f;
            var eliteWeight = eliteReady ? GetEliteDirectorWeight(elapsed) : 0f;
            var fodderWeight = GetFodderDirectorWeight(specialistReady, eliteReady);
            var totalWeight = fodderWeight + specialistWeight + eliteWeight;

            if (totalWeight <= 0f)
            {
                return SpawnClass.Fodder;
            }

            var roll = Random.value * totalWeight;
            if (roll < fodderWeight)
            {
                return SpawnClass.Fodder;
            }

            roll -= fodderWeight;
            if (roll < specialistWeight)
            {
                SpendSpecialistBudget();
                return SpawnClass.Specialist;
            }

            SpendEliteBudget();
            return SpawnClass.Elite;
        }

        private EnemyKind ChooseFodderKind()
        {
            var elapsed = GetElapsedRunTime();
            var watcherAvailable = watcherEyeSprite != null &&
                                   watcherEyeProjectilePrefab != null &&
                                   elapsed >= WatcherEyeFodderUnlockTime;
            var parasiteAvailable = parasiteSprite != null && elapsed >= ParasiteUnlockTime;
            if (!watcherAvailable && !parasiteAvailable)
            {
                return EnemyKind.MireWretch;
            }

            var mireWeight = 1f;
            var watcherWeight = watcherAvailable
                ? Mathf.Lerp(0.2f, 0.45f, Mathf.Clamp01((elapsed - WatcherEyeFodderUnlockTime) / 90f))
                : 0f;
            var parasiteWeight = parasiteAvailable
                ? Mathf.Lerp(0.3f, 0.75f, Mathf.Clamp01((elapsed - ParasiteUnlockTime) / 90f))
                : 0f;
            var totalWeight = mireWeight + watcherWeight + parasiteWeight;
            if (totalWeight <= 0f)
            {
                return EnemyKind.MireWretch;
            }

            var roll = Random.value * totalWeight;
            if (roll < mireWeight)
            {
                return EnemyKind.MireWretch;
            }

            roll -= mireWeight;
            if (roll < watcherWeight)
            {
                return EnemyKind.WatcherEye;
            }

            return EnemyKind.Parasite;
        }

        private EnemyKind ChooseSpecialistKind()
        {
            var elapsed = GetElapsedRunTime();
            var acolyteAvailable = acolyteSprite != null && acolyteProjectilePrefab != null;
            var mermaidAvailable = mermaidSprite != null && elapsed >= MermaidUnlockTime;

            if (!mermaidAvailable || !acolyteAvailable)
            {
                return mermaidAvailable ? EnemyKind.Mermaid : EnemyKind.DrownedAcolyte;
            }

            var mermaidWeight = Mathf.Lerp(0.28f, 0.58f, Mathf.Clamp01((elapsed - MermaidUnlockTime) / 100f));
            return Random.value < mermaidWeight ? EnemyKind.Mermaid : EnemyKind.DrownedAcolyte;
        }

        private EnemyKind ChooseEliteKind()
        {
            return EnemyKind.DeepSpawn;
        }

        private Vector3 BuildSpawnPosition()
        {
            if (player == null)
            {
                return Vector3.zero;
            }

            var direction2D = Random.insideUnitCircle;
            if (direction2D.sqrMagnitude <= 0.001f)
            {
                direction2D = Vector2.right;
            }

            direction2D.Normalize();
            var position = player.position + new Vector3(direction2D.x, 0f, direction2D.y) * spawnRadius;
            position.y = player.position.y + spawnHeightOffset;
            return position;
        }

        private static float GetMaxHealth(EnemyKind enemyKind)
        {
            return enemyKind switch
            {
                EnemyKind.MireWretch => 3f,
                EnemyKind.DrownedAcolyte => 5f,
                EnemyKind.Mermaid => 6f,
                EnemyKind.WatcherEye => 3f,
                EnemyKind.Parasite => 1f,
                EnemyKind.DeepSpawn => 24f,
                _ => 3f
            };
        }

        private Sprite GetSprite(EnemyKind enemyKind)
        {
            return enemyKind switch
            {
                EnemyKind.DrownedAcolyte when acolyteSprite != null => acolyteSprite,
                EnemyKind.Mermaid when mermaidSprite != null => mermaidSprite,
                EnemyKind.WatcherEye when watcherEyeSprite != null => watcherEyeSprite,
                EnemyKind.Parasite when parasiteSprite != null => parasiteSprite,
                EnemyKind.DeepSpawn when deepSpawnSprite != null => deepSpawnSprite,
                _ => mireSprite
            };
        }

        private float GetElapsedRunTime()
        {
            return initialized ? Mathf.Max(0f, Time.time - runtimeStartedAt) : 0f;
        }

        private int GetAliveCountForClass(SpawnClass spawnClass)
        {
            var count = 0;
            foreach (var enemy in activeEnemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                if (ResolveSpawnClass(enemy) == spawnClass)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static SpawnClass ResolveSpawnClass(GameObject enemy)
        {
            if (enemy == null)
            {
                return SpawnClass.Fodder;
            }

            if (enemy.GetComponent<DeepSpawnPrefab>() != null || enemy.GetComponent<DeepSpawnBruiser>() != null)
            {
                return SpawnClass.Elite;
            }

            if (enemy.GetComponent<DrownedAcolyteShooter>() != null || enemy.GetComponent<MermaidController>() != null)
            {
                return SpawnClass.Specialist;
            }

            return SpawnClass.Fodder;
        }

        private static SpawnClass ResolveSpawnClass(EnemyKind enemyKind)
        {
            return enemyKind switch
            {
                EnemyKind.DrownedAcolyte or EnemyKind.Mermaid => SpawnClass.Specialist,
                EnemyKind.DeepSpawn => SpawnClass.Elite,
                _ => SpawnClass.Fodder
            };
        }

        private int GetCurrentSpecialistCap(float elapsedTime)
        {
            var baseCap = 0;
            if (elapsedTime < SpecialistUnlockTime)
            {
                baseCap = 0;
            }
            else if (elapsedTime < 95f)
            {
                baseCap = 1;
            }
            else if (elapsedTime < 180f)
            {
                baseCap = 2;
            }
            else
            {
                baseCap = 3;
            }

            return Mathf.Max(0, baseCap + corruptionSpecialistCapBonus);
        }

        private int GetCurrentEliteCap(float elapsedTime)
        {
            var baseCap = 0;
            if (elapsedTime < EliteUnlockTime)
            {
                baseCap = 0;
            }
            else if (elapsedTime < 190f)
            {
                baseCap = 1;
            }
            else
            {
                baseCap = 2;
            }

            return Mathf.Max(0, baseCap + corruptionEliteCapBonus);
        }

        private float GetSpecialistDirectorWeight(float elapsedTime)
        {
            var timePressure = Mathf.Clamp01((elapsedTime - SpecialistUnlockTime) / 80f);
            return 0.5f + (specialistBudget * 0.65f) + (timePressure * 0.35f);
        }

        private float GetEliteDirectorWeight(float elapsedTime)
        {
            var timePressure = Mathf.Clamp01((elapsedTime - EliteUnlockTime) / 110f);
            return 0.28f + (eliteBudget * 0.75f) + (timePressure * 0.24f);
        }

        private static float GetFodderDirectorWeight(bool specialistReady, bool eliteReady)
        {
            if (specialistReady && eliteReady)
            {
                return 0.95f;
            }

            if (specialistReady || eliteReady)
            {
                return 1.2f;
            }

            return 1.5f;
        }

        private void SpendSpecialistBudget()
        {
            specialistBudget = Mathf.Max(0f, specialistBudget - SpecialistSpawnCost);
            specialistCooldownRemaining = SpecialistBaseCooldown + Random.Range(0f, SpecialistCooldownJitter);
        }

        private void SpendEliteBudget()
        {
            eliteBudget = Mathf.Max(0f, eliteBudget - EliteSpawnCost);
            eliteCooldownRemaining = EliteBaseCooldown + Random.Range(0f, EliteCooldownJitter);
        }

        private void ResetDirectorState()
        {
            specialistBudget = 0f;
            eliteBudget = 0f;
            waveActive = false;
            activeWaveRemainingSpawns = 0;
            activeWaveTotalSpawns = 0;
            activeWaveSpawnIndex = 0;
            activeWaveAliveCapBonus = 0;
            wavesStartedCount = 0;
            activeWaveBurstTimer = 0f;
            scriptedWaves.Clear();
            ResetWaveTimer();
            ResetDirectorCooldowns();
        }

        private void ResetDirectorCooldowns()
        {
            specialistCooldownRemaining = 0f;
            eliteCooldownRemaining = 0f;
        }

        private bool HasAvailableSpecialist(float elapsedTime)
        {
            var acolyteAvailable = acolyteSprite != null && acolyteProjectilePrefab != null;
            var mermaidAvailable = mermaidSprite != null && elapsedTime >= MermaidUnlockTime;
            return acolyteAvailable || mermaidAvailable;
        }

        private bool HasAvailableElite()
        {
            return deepSpawnPrefab != null;
        }

        private void TryStartWave()
        {
            if (!TrySelectWave(out var enemyKind, out var pattern, out var totalSpawns, out var aliveCapBonus))
            {
                ResetWaveTimer();
                return;
            }

            waveActive = true;
            activeWaveEnemyKind = enemyKind;
            activeWavePattern = pattern;
            activeWaveRemainingSpawns = totalSpawns;
            activeWaveTotalSpawns = totalSpawns;
            activeWaveSpawnIndex = 0;
            activeWaveAliveCapBonus = aliveCapBonus;
            wavesStartedCount += 1;
            activeWaveBurstTimer = 0f;
            activeWavePrimaryDirection = ResolveWavePrimaryDirection();
        }

        private void EndWave()
        {
            waveActive = false;
            activeWaveRemainingSpawns = 0;
            activeWaveTotalSpawns = 0;
            activeWaveSpawnIndex = 0;
            activeWaveAliveCapBonus = 0;
            activeWaveBurstTimer = 0f;
            ResetWaveTimer();
        }

        private void ResetWaveTimer()
        {
            waveTimer = WaveBaseInterval + Random.Range(-WaveIntervalJitter, WaveIntervalJitter);
        }

        private bool TriggerCorruptionWave(EnemyKind enemyKind)
        {
            if (!CanSpawnCorruptionWaveEnemy(enemyKind))
            {
                return false;
            }

            var pattern = ChooseWavePattern(enemyKind);
            var totalSpawns = GetWaveSpawnCount(enemyKind, pattern, wavesStartedCount);
            if (totalSpawns <= 0)
            {
                return false;
            }

            wavesStartedCount += 1;
            scriptedWaves.Add(new ScriptedWaveState
            {
                EnemyKind = enemyKind,
                Pattern = pattern,
                PrimaryDirection = ResolveWavePrimaryDirection(),
                BurstTimer = 0f,
                RemainingSpawns = totalSpawns,
                TotalSpawns = totalSpawns,
                SpawnIndex = 0,
                AliveCapBonus = GetWaveAliveCapBonus(enemyKind, wavesStartedCount)
            });

            return true;
        }

        private bool CanSpawnCorruptionWaveEnemy(EnemyKind enemyKind)
        {
            var elapsed = GetElapsedRunTime();
            return enemyKind switch
            {
                EnemyKind.DrownedAcolyte => acolyteSprite != null && acolyteProjectilePrefab != null,
                EnemyKind.Mermaid => mermaidSprite != null && elapsed >= MermaidUnlockTime,
                EnemyKind.WatcherEye => watcherEyeSprite != null && watcherEyeProjectilePrefab != null && elapsed >= WatcherEyeFodderUnlockTime,
                EnemyKind.Parasite => parasiteSprite != null && elapsed >= ParasiteUnlockTime,
                EnemyKind.DeepSpawn => HasAvailableElite(),
                _ => mireSprite != null
            };
        }

        private bool TrySelectWave(out EnemyKind enemyKind, out WavePattern pattern, out int totalSpawns, out int aliveCapBonus)
        {
            enemyKind = EnemyKind.MireWretch;
            pattern = WavePattern.SingleSideLine;
            totalSpawns = 0;
            aliveCapBonus = 0;

            var elapsed = GetElapsedRunTime();
            var defeatedBossCount = runStateManager != null ? runStateManager.BossesDefeatedCount : 0;
            var canUseEliteWaves = (defeatedBossCount >= 4 || corruptionEliteWaveUnlockOverride) && HasAvailableElite();
            var canUseSpecialistWaves = defeatedBossCount >= 2 && HasAvailableSpecialist(elapsed);

            var laneRoll = Random.value;
            var spawnClass = SpawnClass.Fodder;
            if (canUseEliteWaves)
            {
                spawnClass = canUseSpecialistWaves
                    ? laneRoll < 0.5f ? SpawnClass.Elite : laneRoll < 0.8f ? SpawnClass.Specialist : SpawnClass.Fodder
                    : laneRoll < 0.7f ? SpawnClass.Elite : SpawnClass.Fodder;
            }
            else if (canUseSpecialistWaves)
            {
                spawnClass = laneRoll < 0.65f ? SpawnClass.Specialist : SpawnClass.Fodder;
            }

            enemyKind = spawnClass switch
            {
                SpawnClass.Elite => ChooseWaveEliteKind(),
                SpawnClass.Specialist => ChooseWaveSpecialistKind(elapsed),
                _ => ChooseWaveFodderKind(elapsed)
            };

            pattern = ChooseWavePattern(enemyKind);
            totalSpawns = GetWaveSpawnCount(enemyKind, pattern, wavesStartedCount);
            aliveCapBonus = GetWaveAliveCapBonus(enemyKind, wavesStartedCount);
            return totalSpawns > 0;
        }

        private EnemyKind ChooseWaveFodderKind(float elapsed)
        {
            var watcherAvailable = watcherEyeSprite != null &&
                                   watcherEyeProjectilePrefab != null &&
                                   elapsed >= WatcherEyeFodderUnlockTime;
            var parasiteAvailable = parasiteSprite != null && elapsed >= ParasiteUnlockTime;
            var mireWeight = 1f;
            var watcherWeight = watcherAvailable ? 0.7f : 0f;
            var parasiteWeight = parasiteAvailable ? 0.9f : 0f;
            var totalWeight = mireWeight + watcherWeight + parasiteWeight;

            var roll = Random.value * totalWeight;
            if (roll < mireWeight)
            {
                return EnemyKind.MireWretch;
            }

            roll -= mireWeight;
            if (roll < watcherWeight)
            {
                return EnemyKind.WatcherEye;
            }

            return parasiteAvailable ? EnemyKind.Parasite : EnemyKind.MireWretch;
        }

        private EnemyKind ChooseWaveSpecialistKind(float elapsed)
        {
            var acolyteAvailable = acolyteSprite != null && acolyteProjectilePrefab != null;
            var mermaidAvailable = mermaidSprite != null && elapsed >= MermaidUnlockTime;
            if (!mermaidAvailable || !acolyteAvailable)
            {
                return mermaidAvailable ? EnemyKind.Mermaid : EnemyKind.DrownedAcolyte;
            }

            return Random.value < 0.45f ? EnemyKind.Mermaid : EnemyKind.DrownedAcolyte;
        }

        private EnemyKind ChooseWaveEliteKind()
        {
            return EnemyKind.DeepSpawn;
        }

        private WavePattern ChooseWavePattern(EnemyKind enemyKind)
        {
            return enemyKind switch
            {
                EnemyKind.MireWretch => Random.value < 0.34f
                    ? WavePattern.SurroundRing
                    : Random.value < 0.5f
                        ? WavePattern.SurroundLanes
                        : WavePattern.DoubleSidePincer,
                EnemyKind.WatcherEye => WavePattern.SingleSideLine,
                EnemyKind.Parasite => Random.value < 0.5f ? WavePattern.SingleSideLine : WavePattern.DoubleSidePincer,
                EnemyKind.DrownedAcolyte => Random.value < 0.6f ? WavePattern.SingleSideLine : WavePattern.DoubleSidePincer,
                EnemyKind.Mermaid => WavePattern.SingleSideLine,
                EnemyKind.DeepSpawn => WavePattern.SingleSideLine,
                _ => WavePattern.SingleSideLine
            };
        }

        private int GetWaveSpawnCount(EnemyKind enemyKind, WavePattern pattern, int priorWaveCount)
        {
            var baseCount = enemyKind switch
            {
                EnemyKind.MireWretch when pattern == WavePattern.SurroundRing => 16,
                EnemyKind.MireWretch when pattern == WavePattern.SurroundLanes => 16,
                EnemyKind.MireWretch => 14,
                EnemyKind.WatcherEye => 10,
                EnemyKind.Parasite => pattern == WavePattern.DoubleSidePincer ? 12 : 10,
                EnemyKind.DrownedAcolyte => pattern == WavePattern.DoubleSidePincer ? 8 : 6,
                EnemyKind.Mermaid => 6,
                EnemyKind.DeepSpawn => 4,
                _ => 4
            };

            var scaleMultiplier = (1f + (priorWaveCount * 0.08f)) * GetCorruptionWaveSizeMultiplier(enemyKind);
            return Mathf.Max(baseCount, Mathf.CeilToInt(baseCount * scaleMultiplier));
        }

        private float GetCorruptionWaveSizeMultiplier(EnemyKind enemyKind)
        {
            return ResolveSpawnClass(enemyKind) switch
            {
                SpawnClass.Specialist => corruptionSpecialistWaveSizeMultiplier,
                SpawnClass.Elite => corruptionEliteWaveSizeMultiplier,
                _ => corruptionFodderWaveSizeMultiplier
            };
        }

        private static int GetWaveAliveCapBonus(EnemyKind enemyKind, int priorWaveCount)
        {
            var baseBonus = ResolveSpawnClass(enemyKind) switch
            {
                SpawnClass.Elite => 2,
                SpawnClass.Specialist => 4,
                _ => 6
            };

            return baseBonus + Mathf.FloorToInt(priorWaveCount * 0.5f);
        }

        private static float GetWaveBurstInterval(EnemyKind enemyKind)
        {
            return ResolveSpawnClass(enemyKind) switch
            {
                SpawnClass.Elite => 0.8f,
                SpawnClass.Specialist => 0.4f,
                _ => 0.2f
            };
        }

        private bool TrySpawnWaveEnemy()
        {
            if (activeWaveTotalSpawns <= 0)
            {
                return false;
            }

            var position = BuildWaveSpawnPosition(activeWaveSpawnIndex, activeWaveTotalSpawns, activeWavePattern, activeWavePrimaryDirection);
            return TrySpawnSpecificEnemy(activeWaveEnemyKind, position);
        }

        private bool TrySpawnScriptedWaveEnemy(ScriptedWaveState wave)
        {
            if (wave == null || wave.TotalSpawns <= 0)
            {
                return false;
            }

            var position = BuildWaveSpawnPosition(wave.SpawnIndex, wave.TotalSpawns, wave.Pattern, wave.PrimaryDirection);
            return TrySpawnSpecificEnemy(wave.EnemyKind, position);
        }

        private Vector3 BuildWaveSpawnPosition(int spawnIndex, int totalSpawns, WavePattern pattern, Vector2 primaryDirection)
        {
            if (player == null)
            {
                return Vector3.zero;
            }

            var waveRadius = spawnRadius * WaveSpawnRadiusMultiplier;
            var primary = primaryDirection.sqrMagnitude > 0.001f ? primaryDirection.normalized : Vector2.right;
            var baseForward = new Vector3(-primary.x, 0f, -primary.y);
            var baseLateral = new Vector3(-baseForward.z, 0f, baseForward.x);

            Vector3 position;
            switch (pattern)
            {
                case WavePattern.DoubleSidePincer:
                {
                    var leftCount = Mathf.CeilToInt(totalSpawns * 0.5f);
                    var rightCount = totalSpawns - leftCount;
                    var usePrimary = spawnIndex < leftCount;
                    var laneCount = usePrimary ? leftCount : Mathf.Max(1, rightCount);
                    var laneIndex = usePrimary ? spawnIndex : spawnIndex - leftCount;
                    var side = usePrimary ? primary : -primary;
                    var anchor = player.position + new Vector3(side.x, 0f, side.y) * waveRadius;
                    var toPlayer = player.position - anchor;
                    toPlayer.y = 0f;
                    var forward = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector3.forward;
                    var lateral = new Vector3(-forward.z, 0f, forward.x);
                    position = anchor + lateral * ResolveLineOffset(laneIndex, laneCount, 1.35f);
                    break;
                }
                case WavePattern.SurroundRing:
                {
                    var baseAngle = Mathf.Atan2(primary.y, primary.x) * Mathf.Rad2Deg;
                    var angle = baseAngle + ((360f / Mathf.Max(1, totalSpawns)) * spawnIndex);
                    var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.right;
                    position = player.position + direction * waveRadius;
                    break;
                }
                case WavePattern.SurroundLanes:
                {
                    var lane = spawnIndex % 4;
                    var laneIndex = spawnIndex / 4;
                    var perLaneCount = Mathf.CeilToInt(totalSpawns / 4f);
                    var side = Rotate(primary, lane * 90f);
                    var anchor = player.position + new Vector3(side.x, 0f, side.y) * waveRadius;
                    var toPlayer = player.position - anchor;
                    toPlayer.y = 0f;
                    var forward = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : baseForward;
                    var lateral = new Vector3(-forward.z, 0f, forward.x);
                    position = anchor + lateral * ResolveLineOffset(laneIndex, perLaneCount, 1.2f);
                    break;
                }
                case WavePattern.SingleSideLine:
                default:
                {
                    var anchor = player.position + new Vector3(primary.x, 0f, primary.y) * waveRadius;
                    position = anchor + baseLateral * ResolveLineOffset(spawnIndex, totalSpawns, 1.35f);
                    break;
                }
            }

            position.y = player.position.y + spawnHeightOffset;
            return position;
        }

        private static float ResolveLineOffset(int index, int count, float spacing)
        {
            var halfWidth = (count - 1) * 0.5f;
            return (index - halfWidth) * spacing;
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var sin = Mathf.Sin(radians);
            var cos = Mathf.Cos(radians);
            return new Vector2(
                (vector.x * cos) - (vector.y * sin),
                (vector.x * sin) + (vector.y * cos));
        }

        private static Vector2 ResolveWavePrimaryDirection()
        {
            var direction = Random.insideUnitCircle;
            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector2.right;
            }

            return direction.normalized;
        }

        private static Sprite LoadBiomeSprite(string preferredPath, string fallbackPath, Sprite fallbackSprite, float pixelsPerUnit = 256f)
        {
            if (!string.IsNullOrWhiteSpace(preferredPath))
            {
                var preferred = RuntimeSpriteLibrary.LoadSprite(preferredPath, pixelsPerUnit);
                if (preferred != null)
                {
                    return preferred;
                }
            }

            var fallback = RuntimeSpriteLibrary.LoadSprite(fallbackPath, pixelsPerUnit);
            return fallback != null ? fallback : fallbackSprite;
        }

        private void RegisterEnemy(GameObject enemy)
        {
            if (enemy == null)
            {
                return;
            }

            activeEnemies.Add(enemy);
        }

        private void UnregisterEnemy(GameObject enemy)
        {
            if (enemy == null)
            {
                return;
            }

            activeEnemies.Remove(enemy);
        }

        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new();

            public static List<T> Get()
            {
                if (Pool.Count > 0)
                {
                    return Pool.Pop();
                }

                return new List<T>();
            }

            public static void Release(List<T> list)
            {
                if (list == null)
                {
                    return;
                }

                list.Clear();
                Pool.Push(list);
            }
        }
    }
}
