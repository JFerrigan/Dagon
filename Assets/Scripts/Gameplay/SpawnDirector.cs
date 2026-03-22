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
        private const string DeepSpawnPrefabResourcePath = "Prefabs/Enemies/DeepSpawn";

        private enum EnemyKind
        {
            MireWretch,
            DrownedAcolyte,
            Mermaid,
            WatcherEye,
            DeepSpawn
        }

        [SerializeField] private Transform player;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Sprite mireSprite;
        [SerializeField] private Sprite acolyteSprite;
        [SerializeField] private Sprite mermaidSprite;
        [SerializeField] private Sprite watcherEyeSprite;
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
        private float bossPhaseIntervalMultiplier = 1f;
        private int defeatedEnemies;
        private int totalSpawned;
        private bool spawningStopped;
        private bool quotaNotified;
        private bool initialized;
        private RuntimeBiomeProfile currentBiomeProfile;
        private bool bossPhaseSpawnThrottleActive;
        private int bossPhaseAliveCap;
        private readonly HashSet<GameObject> activeEnemies = new();

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

            DespawnFarEnemies();

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
            mireSprite = enemySprite;
            acolyteSprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Enemies/drowned_acolyte", 256f);
            mermaidSprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Enemies/mermaid", 64f);
            watcherEyeSprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Enemies/watcher_eye", 64f);
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

        public void IncreaseSandboxPressure(float intervalReduction = 0.18f, int additionalAliveCap = 1)
        {
            TightenPressure(intervalReduction, additionalAliveCap);
            spawnTimer = Mathf.Min(spawnTimer, GetCurrentMinSpawnInterval());
        }

        public bool SpawnSandboxMireWretch()
        {
            return TrySpawnSpecificEnemy(EnemyKind.MireWretch, BuildSpawnPosition(), ignoreAliveCap: true);
        }

        public bool SpawnSandboxDrownedAcolyte()
        {
            return TrySpawnSpecificEnemy(EnemyKind.DrownedAcolyte, BuildSpawnPosition(), ignoreAliveCap: true);
        }

        public bool SpawnSandboxMermaid()
        {
            return TrySpawnSpecificEnemy(EnemyKind.Mermaid, BuildSpawnPosition(), ignoreAliveCap: true);
        }

        public bool SpawnSandboxWatcherEye()
        {
            return TrySpawnSpecificEnemy(EnemyKind.WatcherEye, BuildSpawnPosition(), ignoreAliveCap: true);
        }

        public bool SpawnSandboxDeepSpawn()
        {
            return TrySpawnSpecificEnemy(EnemyKind.DeepSpawn, BuildSpawnPosition(), ignoreAliveCap: true);
        }

        public void ConfigureBiome(RuntimeBiomeProfile profile)
        {
            currentBiomeProfile = profile;
            mireSprite = LoadBiomeSprite(profile != null ? profile.MireWretchSpritePath : null, "Sprites/Enemies/mire_wretch", mireSprite);
            acolyteSprite = LoadBiomeSprite(profile != null ? profile.DrownedAcolyteSpritePath : null, "Sprites/Enemies/drowned_acolyte", acolyteSprite);
            mermaidSprite = LoadBiomeSprite(profile != null ? profile.MermaidSpritePath : null, "Sprites/Enemies/mermaid", mermaidSprite, 64f);
            watcherEyeSprite = LoadBiomeSprite(profile != null ? profile.WatcherEyeSpritePath : null, "Sprites/Enemies/watcher_eye", watcherEyeSprite, 64f);
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

            var spawnedAny = TrySpawnSpecificEnemy(ChooseNextEnemyKind(), BuildSpawnPosition());
            if (SpawnQuotaMet)
            {
                NotifyQuotaCompletedIfNeeded();
            }

            return spawnedAny;
        }

        private bool TrySpawnSpecificEnemy(EnemyKind enemyKind, Vector3 position, bool ignoreAliveCap = false)
        {
            if (player == null || mireSprite == null || SpawnQuotaMet || totalSpawned >= regularSpawnQuota)
            {
                return false;
            }

            if (!ignoreAliveCap && activeEnemies.Count >= GetCurrentMaxAliveEnemies())
            {
                return false;
            }

            if (enemyKind == EnemyKind.DeepSpawn && TrySpawnDeepSpawn(position))
            {
                return true;
            }

            var mire = new GameObject($"{enemyKind}_{activeEnemies.Count + 1}");
            mire.transform.SetParent(transform);
            mire.transform.position = position;

            var collider = mire.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            ConfigureCollider(collider, enemyKind);

            var rigidbody = mire.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            var health = mire.AddComponent<Health>();
            health.SetMaxHealth(GetMaxHealth(enemyKind), true);
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
                    contactDamage.Configure(1f);

                    var wanderer = mire.AddComponent<MireWanderer>();
                    wanderer.Configure(player, Random.Range(3.2f, 3.6f), 3f, 18f);
                    rewards.Configure(1, 1.5f);
                    break;
                }
                case EnemyKind.DrownedAcolyte:
                {
                    knockbackReceiver.Configure(0.85f, 18f, 5f);
                    var shooter = mire.AddComponent<DrownedAcolyteShooter>();
                    shooter.Configure(player, acolyteProjectilePrefab, Random.Range(2.4f, 2.8f), 6f, 1.6f, worldCamera);
                    rewards.Configure(3, 3f);
                    break;
                }
                case EnemyKind.Mermaid:
                {
                    knockbackReceiver.Configure(0.8f, 18f, 5f);
                    var mermaid = mire.AddComponent<MermaidController>();
                    mermaid.Configure(player, worldCamera, Random.Range(2.1f, 2.4f), 7.1f);
                    rewards.Configure(4, 4.5f);
                    break;
                }
                case EnemyKind.WatcherEye:
                {
                    knockbackReceiver.Configure(0.7f, 18f, 5f);
                    var watcherEye = mire.AddComponent<WatcherEyeController>();
                    watcherEye.Configure(player, watcherEyeProjectilePrefab, worldCamera, Random.Range(2.2f, 2.5f), 7.8f);
                    rewards.Configure(1, 1.75f);
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
                    rewards.Configure(6, 7f);
                    break;
                }
            }

            ConfigureVisuals(mire.transform, enemyKind);
            ConfigureHealthBar(mire.transform, health, enemyKind);
            RegisterEnemy(mire);
            totalSpawned += 1;
            return true;
        }

        private void ConfigureVisuals(Transform enemyRoot, EnemyKind enemyKind)
        {
            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(enemyRoot, false);
            visuals.transform.localPosition = Vector3.zero;

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSprite(enemyKind);
            renderer.sortingOrder = enemyKind == EnemyKind.DeepSpawn ? 7 : enemyKind == EnemyKind.MireWretch ? 5 : 6;
            renderer.color = currentBiomeProfile != null ? currentBiomeProfile.EnemyTint : Color.white;

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
            else if (enemyKind == EnemyKind.DrownedAcolyte && acolyteSprite != null)
            {
                visuals.transform.localScale = new Vector3(0.82f, 0.82f, 1f);
            }

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);
        }

        private bool TrySpawnDeepSpawn(Vector3 position)
        {
            if (deepSpawnPrefab == null)
            {
                return false;
            }

            var deepSpawnObject = Instantiate(deepSpawnPrefab, position, Quaternion.identity, transform);
            deepSpawnObject.name = $"{EnemyKind.DeepSpawn}_{activeEnemies.Count + 1}";

            var deepSpawn = deepSpawnObject.GetComponent<DeepSpawnPrefab>();
            if (deepSpawn == null)
            {
                Destroy(deepSpawnObject);
                return false;
            }

            deepSpawn.Configure(player, worldCamera, enemyHealthBarsAlwaysVisible, enemyHealthBarVisibleDuration);

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
            spawnTimer = Random.Range(currentMinInterval, currentMaxInterval) * bossPhaseIntervalMultiplier;
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
            var nextSpawnIndex = totalSpawned + 1;
            var deepSpawnEvery = currentBiomeProfile != null ? currentBiomeProfile.DeepSpawnSpawnEvery : eliteSpawnEvery;
            var acolyteEvery = currentBiomeProfile != null ? currentBiomeProfile.DrownedAcolyteSpawnEvery : 5;
            var mermaidEvery = currentBiomeProfile != null ? currentBiomeProfile.MermaidSpawnEvery : 8;
            var watcherEyeEvery = currentBiomeProfile != null ? currentBiomeProfile.WatcherEyeSpawnEvery : 6;

            if (deepSpawnEvery > 0 &&
                nextSpawnIndex > 1 &&
                nextSpawnIndex % deepSpawnEvery == 0 &&
                deepSpawnPrefab != null)
            {
                return EnemyKind.DeepSpawn;
            }

            var acolyteEligible = acolyteEvery > 0 && nextSpawnIndex % acolyteEvery == 0 && acolyteProjectilePrefab != null;
            var mermaidEligible = mermaidSprite != null && mermaidEvery > 0 && nextSpawnIndex >= mermaidEvery && nextSpawnIndex % mermaidEvery == 0;
            var watcherEyeEligible = watcherEyeSprite != null &&
                                     watcherEyeProjectilePrefab != null &&
                                     watcherEyeEvery > 0 &&
                                     nextSpawnIndex >= watcherEyeEvery &&
                                     nextSpawnIndex % watcherEyeEvery == 0;

            var specialistCount = 0;
            specialistCount += acolyteEligible ? 1 : 0;
            specialistCount += mermaidEligible ? 1 : 0;

            if (specialistCount > 1)
            {
                var elapsed = Mathf.Max(0f, Time.time - runtimeStartedAt - 20f);
                var mermaidWeight = Mathf.Lerp(0.35f, 0.6f, Mathf.Clamp01(elapsed / 100f));
                var totalWeight = 0f;
                totalWeight += acolyteEligible ? 1f : 0f;
                totalWeight += mermaidEligible ? mermaidWeight : 0f;

                var roll = Random.value * totalWeight;
                if (acolyteEligible)
                {
                    if (roll < 1f)
                    {
                        return EnemyKind.DrownedAcolyte;
                    }

                    roll -= 1f;
                }

                if (mermaidEligible)
                {
                    if (roll < mermaidWeight)
                    {
                        return EnemyKind.Mermaid;
                    }

                    roll -= mermaidWeight;
                }
            }

            if (acolyteEligible)
            {
                return EnemyKind.DrownedAcolyte;
            }

            if (mermaidEligible)
            {
                return EnemyKind.Mermaid;
            }

            if (watcherEyeEligible)
            {
                return EnemyKind.WatcherEye;
            }

            return EnemyKind.MireWretch;
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
                EnemyKind.DeepSpawn when deepSpawnSprite != null => deepSpawnSprite,
                _ => mireSprite
            };
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
