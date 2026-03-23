using System.Collections.Generic;
using Dagon.Bootstrap;
using Dagon.Core;
using Dagon.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RunStateManager : MonoBehaviour
    {
        public readonly struct BossVisualSpec
        {
            public BossVisualSpec(string resourcePath, float pixelsPerUnit, Vector3 scale, Vector3 localPosition)
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

        private enum BossKind
        {
            MireColossus,
            Monolith,
            DrownedAdmiral
        }

        private static readonly BossKind[] AllBossKinds =
        {
            BossKind.MireColossus,
            BossKind.Monolith,
            BossKind.DrownedAdmiral
        };

        private readonly struct BossRuntimeDefinition
        {
            public BossRuntimeDefinition(
                BossKind bossKind,
                string objectName,
                string displayName,
                string spritePath,
                Color tint,
                float maxHealth,
                Vector3 colliderCenter,
                float colliderHeight,
                float colliderRadius,
                Vector3 healthBarOffset,
                Vector3 visualScale,
                Vector3 visualOffset,
                float spawnHeightOffset,
                float wideSummonCooldown,
                float tallSummonCooldown,
                int maxWideLeeches,
                int maxTallLeeches,
                int difficultyTier,
                int experienceReward,
                float corruptionReward)
            {
                BossKind = bossKind;
                ObjectName = objectName;
                DisplayName = displayName;
                SpritePath = spritePath;
                Tint = tint;
                MaxHealth = maxHealth;
                ColliderCenter = colliderCenter;
                ColliderHeight = colliderHeight;
                ColliderRadius = colliderRadius;
                HealthBarOffset = healthBarOffset;
                VisualScale = visualScale;
                VisualOffset = visualOffset;
                SpawnHeightOffset = spawnHeightOffset;
                WideSummonCooldown = wideSummonCooldown;
                TallSummonCooldown = tallSummonCooldown;
                MaxWideLeeches = maxWideLeeches;
                MaxTallLeeches = maxTallLeeches;
                DifficultyTier = difficultyTier;
                ExperienceReward = experienceReward;
                CorruptionReward = corruptionReward;
            }

            public BossKind BossKind { get; }
            public string ObjectName { get; }
            public string DisplayName { get; }
            public string SpritePath { get; }
            public Color Tint { get; }
            public float MaxHealth { get; }
            public Vector3 ColliderCenter { get; }
            public float ColliderHeight { get; }
            public float ColliderRadius { get; }
            public Vector3 HealthBarOffset { get; }
            public Vector3 VisualScale { get; }
            public Vector3 VisualOffset { get; }
            public float SpawnHeightOffset { get; }
            public float WideSummonCooldown { get; }
            public float TallSummonCooldown { get; }
            public int MaxWideLeeches { get; }
            public int MaxTallLeeches { get; }
            public int DifficultyTier { get; }
            public int ExperienceReward { get; }
            public float CorruptionReward { get; }
        }

        [SerializeField] private Transform player;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private SpawnDirector spawnDirector;
        [SerializeField] private CorruptionMeter corruptionMeter;
        [SerializeField] private ExperienceController experienceController;
        [SerializeField] private HarpoonProjectile bossProjectilePrefab;
        [SerializeField] private string nextSceneName;
        [SerializeField] private string menuSceneName = "MainMenu";
        [SerializeField] private int bossesInWave = 1;
        [SerializeField] private bool useTimedBossTransition;
        [SerializeField] private float bossTransitionDelaySeconds = 45f;
        [SerializeField] private bool showSpawnProgressUi = true;
        [SerializeField] private bool endRunOnBossDefeat = true;

        private readonly List<Health> activeBosses = new();
        private readonly List<BossKind> remainingBossKinds = new();
        private Health playerHealth;
        private Texture2D whiteTexture;
        private GUIStyle endTitleStyle;
        private GUIStyle endBodyStyle;
        private GUIStyle endButtonStyle;
        private float runTimer;
        private float biomeTimer;
        private bool bossWaveStarted;
        private bool runEnded;
        private bool playerWon;
        private bool pauseMenuOpen;
        private float bossWaveBannerTimer;
        private bool bossTransitionArmed;
        private bool allowAmbientSpawningDuringBoss;
        private float bossAmbientSpawnIntervalMultiplier = 1f;
        private int bossAmbientAliveCap = 0;
        private int bossesDefeatedCount;
        private string biomeBossDisplayName = "Mire Colossus";
        private Color biomeBossTint = Color.white;
        private string biomeBossSpritePath = "Sprites/Bosses/mire_colossus";
        private string currentBossDisplayName = "Mire Colossus";
        private string currentBiomeDisplayName = "Black Mire";
        private Color currentBossTint = Color.white;
        private string currentBossSpritePath = "Sprites/Bosses/mire_colossus";

        public float RunTimer => runTimer;
        public bool RunEnded => runEnded;
        public bool BossWaveStarted => bossWaveStarted;
        public int BossesDefeatedCount => bossesDefeatedCount;
        public bool PauseMenuOpen => pauseMenuOpen;
        public event System.Action BossWaveCompleted;

        public static bool TryGetBossVisualSpec(string bossId, out BossVisualSpec spec)
        {
            switch (bossId)
            {
                case "mire_colossus":
                    spec = new BossVisualSpec("Sprites/Bosses/mire_colossus", 256f, new Vector3(0.95f, 0.95f, 1f), Vector3.zero);
                    return true;
                case "monolith":
                    spec = new BossVisualSpec("Sprites/Bosses/monolith", 256f, new Vector3(7.75f, 7.75f, 1f), new Vector3(0f, -3.2f, 0f));
                    return true;
                case "drowned_admiral":
                    spec = new BossVisualSpec("Sprites/Bosses/admiral", 256f, new Vector3(2.7f, 2.7f, 1f), Vector3.zero);
                    return true;
                default:
                    spec = default;
                    return false;
            }
        }

        private void Start()
        {
            whiteTexture = Texture2D.whiteTexture;
            if (experienceController == null)
            {
                experienceController = FindObjectOfType<ExperienceController>();
            }

            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
                corruptionMeter = player.GetComponent<CorruptionMeter>();
                if (playerHealth != null)
                {
                    playerHealth.Died += HandlePlayerDied;
                }
            }

            if (spawnDirector != null)
            {
                spawnDirector.BattlefieldCleared += HandleBattlefieldCleared;
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.Died -= HandlePlayerDied;
            }

            if (spawnDirector != null)
            {
                spawnDirector.BattlefieldCleared -= HandleBattlefieldCleared;
            }

            for (var i = 0; i < activeBosses.Count; i++)
            {
                if (activeBosses[i] != null)
                {
                    activeBosses[i].Died -= HandleBossDied;
                }
            }
        }

        private void Update()
        {
            if (!runEnded && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (!IsUpgradeOverlayOpen())
                {
                    TogglePauseMenu();
                }
            }

            if (runEnded)
            {
                return;
            }

            if (Time.timeScale <= 0f)
            {
                return;
            }

            runTimer += Time.deltaTime;
            biomeTimer += Time.deltaTime;
            bossWaveBannerTimer = Mathf.Max(0f, bossWaveBannerTimer - Time.deltaTime);
            if (bossWaveStarted || spawnDirector == null)
            {
                return;
            }

            if (useTimedBossTransition)
            {
                if (!bossTransitionArmed && biomeTimer >= bossTransitionDelaySeconds)
                {
                    bossTransitionArmed = true;
                    spawnDirector.StopSpawning();
                    BeginBossWave();
                    Debug.Log(
                        $"RunStateManager armed boss transition at run={runTimer:0.0}s biome={biomeTimer:0.0}s in scene '{SceneManager.GetActiveScene().name}'.",
                        this);
                }

                return;
            }

            if (spawnDirector.IsBattlefieldClear)
            {
                BeginBossWave();
            }
        }

        public void Configure(Transform playerTransform, Camera cameraReference, SpawnDirector director, Sprite enemySprite, HarpoonProjectile projectilePrefab)
        {
            player = playerTransform;
            worldCamera = cameraReference;
            spawnDirector = director;
            bossProjectilePrefab = projectilePrefab;
            corruptionMeter = player != null ? player.GetComponent<CorruptionMeter>() : corruptionMeter;
        }

        public void ConfigureLevelFlow(string newNextSceneName, string newMenuSceneName, int newBossesInWave)
        {
            nextSceneName = newNextSceneName;
            if (!string.IsNullOrWhiteSpace(newMenuSceneName))
            {
                menuSceneName = newMenuSceneName;
            }

            bossesInWave = Mathf.Max(1, newBossesInWave);
        }

        public void ConfigureBossTransition(bool useTimedTransition, float timedTransitionDelaySeconds, bool showSpawnProgress)
        {
            useTimedBossTransition = useTimedTransition;
            bossTransitionDelaySeconds = Mathf.Max(1f, timedTransitionDelaySeconds);
            showSpawnProgressUi = showSpawnProgress;
            bossTransitionArmed = false;
        }

        public void ConfigureBossResolution(bool shouldEndRunOnBossDefeat)
        {
            endRunOnBossDefeat = shouldEndRunOnBossDefeat;
        }

        public void ConfigureBossAmbientSpawning(bool allowAmbientSpawns, float intervalMultiplier, int aliveCap)
        {
            allowAmbientSpawningDuringBoss = allowAmbientSpawns;
            bossAmbientSpawnIntervalMultiplier = Mathf.Max(1f, intervalMultiplier);
            bossAmbientAliveCap = Mathf.Max(0, aliveCap);
        }

        public bool SpawnSandboxBoss()
        {
            return SpawnSandboxBoss(BossKind.MireColossus, false);
        }

        public bool SpawnSandboxMonolithBoss()
        {
            return SpawnSandboxBoss(BossKind.Monolith, false);
        }

        public bool SpawnSandboxAdmiralBoss()
        {
            return SpawnSandboxBoss(BossKind.DrownedAdmiral, false);
        }

        public bool SpawnSandboxBoss(bool forceCorrupted)
        {
            return SpawnSandboxBoss(BossKind.MireColossus, forceCorrupted);
        }

        public bool SpawnSandboxMonolithBoss(bool forceCorrupted)
        {
            return SpawnSandboxBoss(BossKind.Monolith, forceCorrupted);
        }

        public bool SpawnSandboxAdmiralBoss(bool forceCorrupted)
        {
            return SpawnSandboxBoss(BossKind.DrownedAdmiral, forceCorrupted);
        }

        private bool SpawnSandboxBoss(BossKind bossKind, bool forceCorrupted)
        {
            if (player == null || worldCamera == null)
            {
                return false;
            }

            SpawnBoss(activeBosses.Count, Mathf.Max(1, activeBosses.Count + 1), ResolveBossDefinition(bossKind, bossesDefeatedCount), forceCorrupted);
            return activeBosses.Count > 0;
        }

        public void ConfigureBiome(RuntimeBiomeProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            biomeBossDisplayName = string.IsNullOrWhiteSpace(profile.BossDisplayName) ? biomeBossDisplayName : profile.BossDisplayName;
            currentBiomeDisplayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? currentBiomeDisplayName : profile.DisplayName;
            biomeBossTint = profile.BossTint;
            biomeBossSpritePath = string.IsNullOrWhiteSpace(profile.BossSpritePath) ? biomeBossSpritePath : profile.BossSpritePath;
            bossTransitionDelaySeconds = Mathf.Max(1f, profile.BossTransitionDelaySeconds);
        }

        public void ResumeAmbientRun(float nextBossDelaySeconds)
        {
            bossWaveStarted = false;
            bossTransitionArmed = false;
            bossWaveBannerTimer = 0f;
            biomeTimer = 0f;
            bossTransitionDelaySeconds = Mathf.Max(1f, nextBossDelaySeconds);
        }

        private void HandleBattlefieldCleared()
        {
            if (useTimedBossTransition)
            {
                if (bossTransitionArmed && !bossWaveStarted && !runEnded)
                {
                    BeginBossWave();
                }

                return;
            }

            if (!bossWaveStarted && !runEnded)
            {
                BeginBossWave();
            }
        }

        private void BeginBossWave()
        {
            if (player == null || worldCamera == null || bossWaveStarted)
            {
                return;
            }

            bossWaveStarted = true;
            bossWaveBannerTimer = 3.2f;
            if (allowAmbientSpawningDuringBoss)
            {
                spawnDirector?.EnterBossAmbientPressure(bossAmbientSpawnIntervalMultiplier, bossAmbientAliveCap);
            }
            else
            {
                spawnDirector?.StopSpawning();
            }

            for (var i = 0; i < bossesInWave; i++)
            {
                var bossKind = SelectNextBossKind();
                SpawnBoss(i, bossesInWave, ResolveBossDefinition(bossKind, bossesDefeatedCount));
            }
        }

        private void SpawnBoss(int index, int totalBosses, BossRuntimeDefinition definition, bool forceCorrupted = false)
        {
            var isCorrupted = forceCorrupted || ShouldSpawnCorruptedBoss();
            var modifiers = isCorrupted ? CorruptionVariantRules.GetBossModifiers() : new CorruptionVariantRules.StatModifiers(1f, 1f, 1f, 1f);
            currentBossDisplayName = isCorrupted ? $"Corrupted {definition.DisplayName}" : definition.DisplayName;
            currentBossTint = definition.Tint;

            var bossSprite = RuntimeSpriteLibrary.LoadSprite(definition.SpritePath, 256f) ??
                RuntimeSpriteLibrary.LoadSprite("Sprites/Bosses/mire_colossus", 256f);
            if (bossSprite == null)
            {
                return;
            }

            var boss = new GameObject(isCorrupted ? $"Corrupted{definition.ObjectName}" : definition.ObjectName);
            boss.transform.SetParent(transform);
            var angle = totalBosses <= 1 ? 0f : (360f / totalBosses) * index;
            var offset = Quaternion.Euler(0f, angle, 0f) * (Vector3.forward * 12f);
            boss.transform.position = player.position + new Vector3(offset.x, definition.SpawnHeightOffset, offset.z);

            var collider = boss.AddComponent<CapsuleCollider>();
            collider.center = definition.ColliderCenter;
            collider.height = definition.ColliderHeight;
            collider.radius = definition.ColliderRadius;
            collider.isTrigger = true;
            boss.AddComponent<BodyBlocker>().Configure(
                BodyBlocker.BodyTeam.Enemy,
                Mathf.Max(0.65f, definition.ColliderRadius * 0.9f),
                definition.ColliderHeight,
                3.25f,
                true,
                true,
                definition.BossKind == BossKind.Monolith);

            var rigidbody = boss.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            var bossHealth = boss.AddComponent<Health>();
            bossHealth.SetMaxHealth(definition.MaxHealth * modifiers.HealthMultiplier, true);
            bossHealth.Died += HandleBossDied;
            activeBosses.Add(bossHealth);
            boss.AddComponent<Hurtbox>().Configure(CombatTeam.Enemy, bossHealth);
            boss.AddComponent<KnockbackReceiver>().Configure(
                definition.BossKind == BossKind.Monolith ? 0f : 0.18f,
                22f,
                2.4f);

            if (definition.BossKind == BossKind.MireColossus)
            {
                var contactDamage = boss.AddComponent<ContactDamage>();
                contactDamage.Configure(4f * modifiers.DamageMultiplier);

                var controller = boss.AddComponent<MireColossusController>();
                controller.Configure(player, bossProjectilePrefab, definition.DifficultyTier);
                if (isCorrupted)
                {
                    controller.ApplyCorruptionModifiers(modifiers.DamageMultiplier, modifiers.SpeedMultiplier, modifiers.CadenceMultiplier);
                }
            }
            else if (definition.BossKind == BossKind.Monolith)
            {
                var controller = boss.AddComponent<MonolithBossController>();
                controller.Configure(
                    player,
                    worldCamera,
                    bossProjectilePrefab,
                    definition.WideSummonCooldown,
                    definition.TallSummonCooldown,
                    definition.MaxWideLeeches,
                    definition.MaxTallLeeches);
                if (isCorrupted)
                {
                    controller.ApplyCorruptionModifiers(modifiers.CadenceMultiplier);
                }
            }
            else
            {
                var contactDamage = boss.AddComponent<ContactDamage>();
                contactDamage.Configure(2f * modifiers.DamageMultiplier);

                var controller = boss.AddComponent<DrownedAdmiralController>();
                controller.Configure(player, worldCamera, bossProjectilePrefab, definition.DifficultyTier);
                if (isCorrupted)
                {
                    controller.ApplyCorruptionModifiers(modifiers.DamageMultiplier, modifiers.SpeedMultiplier, modifiers.CadenceMultiplier);
                }
            }

            var rewards = boss.AddComponent<EnemyDeathRewards>();
            rewards.Configure(definition.ExperienceReward, definition.CorruptionReward);

            var healthBar = boss.AddComponent<EnemyHealthBar>();
            healthBar.Configure(worldCamera, definition.HealthBarOffset, false);

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(boss.transform, false);
            visuals.transform.localPosition = definition.VisualOffset;

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = bossSprite;
            renderer.sortingOrder = 20;
            renderer.color = definition.Tint;
            visuals.transform.localScale = definition.VisualScale;

            if (isCorrupted)
            {
                visuals.AddComponent<CorruptedVariantVisual>().Apply(renderer, definition.Tint);
            }

            var billboard = visuals.AddComponent<Dagon.Rendering.BillboardSprite>();
            billboard.Configure(worldCamera, Dagon.Rendering.BillboardSprite.BillboardMode.YAxisOnly);
        }

        private bool ShouldSpawnCorruptedBoss()
        {
            if (corruptionMeter == null && player != null)
            {
                corruptionMeter = player.GetComponent<CorruptionMeter>();
            }

            if (corruptionMeter == null)
            {
                return false;
            }

            return Random.value <= CorruptionVariantRules.GetBossCorruptionChance(corruptionMeter.CurrentCorruption);
        }

        private BossRuntimeDefinition ResolveBossDefinition(BossKind bossKind, int defeatedBossCount)
        {
            var healthMultiplier = 1f + (defeatedBossCount * 0.75f) + (defeatedBossCount * defeatedBossCount * 0.20f);
            return bossKind switch
            {
                BossKind.Monolith => new BossRuntimeDefinition(
                    BossKind.Monolith,
                    "MonolithBoss",
                    "Monolith of the Mire",
                    "Sprites/Bosses/monolith",
                    new Color(0.88f, 0.96f, 0.88f, 1f),
                    180f * healthMultiplier,
                    new Vector3(0f, 6.2f, 0f),
                    12.4f,
                    3.9f,
                    new Vector3(0f, 7.6f, 0f),
                    new Vector3(7.75f, 7.75f, 1f),
                    new Vector3(0f, -3.2f, 0f),
                    0f,
                    Mathf.Max(0.8f, 2.4f - (defeatedBossCount * 0.22f)),
                    Mathf.Max(1.6f, 4.9f - (defeatedBossCount * 0.3f)),
                    6 + defeatedBossCount,
                    3 + Mathf.FloorToInt(defeatedBossCount * 0.5f),
                    defeatedBossCount,
                    20,
                    20f),
                BossKind.DrownedAdmiral => new BossRuntimeDefinition(
                    BossKind.DrownedAdmiral,
                    "DrownedAdmiral",
                    "Drowned Admiral",
                    "Sprites/Bosses/admiral",
                    new Color(0.90f, 0.96f, 0.88f, 1f),
                    70f * healthMultiplier,
                    new Vector3(0f, 1.2f, 0f),
                    2.8f,
                    0.75f,
                    new Vector3(0f, 2.55f, 0f),
                    new Vector3(2.7f, 2.7f, 1f),
                    Vector3.zero,
                    0f,
                    0f,
                    0f,
                    0,
                    0,
                    defeatedBossCount,
                    20,
                    20f),
                _ => new BossRuntimeDefinition(
                    BossKind.MireColossus,
                    "MireColossus",
                    biomeBossDisplayName,
                    biomeBossSpritePath,
                    biomeBossTint,
                    35f * healthMultiplier,
                    new Vector3(0f, 1.15f, 0f),
                    3.2f,
                    1.15f,
                    new Vector3(0f, 2.45f, 0f),
                    new Vector3(0.95f, 0.95f, 1f),
                    Vector3.zero,
                    0f,
                    0f,
                    0f,
                    0,
                    0,
                    defeatedBossCount,
                    20,
                    20f)
            };
        }

        private BossKind SelectNextBossKind()
        {
            if (remainingBossKinds.Count == 0)
            {
                remainingBossKinds.AddRange(AllBossKinds);
            }

            var selectedIndex = Random.Range(0, remainingBossKinds.Count);
            var selectedKind = remainingBossKinds[selectedIndex];
            remainingBossKinds.RemoveAt(selectedIndex);
            return selectedKind;
        }

        private void HandlePlayerDied(Health health, GameObject source)
        {
            EndRun(false);
        }

        private void HandleBossDied(Health health, GameObject source)
        {
            health.Died -= HandleBossDied;
            activeBosses.Remove(health);
            bossesDefeatedCount += 1;
            if (bossWaveStarted && activeBosses.Count == 0)
            {
                if (endRunOnBossDefeat)
                {
                    EndRun(true);
                    return;
                }

                BossWaveCompleted?.Invoke();
            }
        }

        private void EndRun(bool won)
        {
            if (runEnded)
            {
                return;
            }

            pauseMenuOpen = false;
            runEnded = true;
            playerWon = won;
            Time.timeScale = 0f;
        }

        private void OnGUI()
        {
            EnsureStyles();
            var hasActiveBoss = activeBosses.Count > 0;

            if (runEnded)
            {
                DrawEndScreen();
                return;
            }

            if (pauseMenuOpen)
            {
                DrawPauseMenu();
                return;
            }

            GUI.Label(new Rect(Screen.width - 220f, 18f, 200f, 22f), $"Run: {runTimer:0.0}s");
            if (!bossWaveStarted && !hasActiveBoss)
            {
                if (showSpawnProgressUi)
                {
                    var enemiesLeft = spawnDirector != null ? spawnDirector.RemainingSpawns : 0;
                    GUI.Label(new Rect(Screen.width - 220f, 40f, 200f, 22f), $"Kills Left: {enemiesLeft}");
                }

                GUI.Label(new Rect(Screen.width - 220f, 62f, 200f, 22f), $"Biome: {currentBiomeDisplayName}");
            }
            else
            {
                DrawBossHealthBar();
                if (bossWaveStarted)
                {
                    DrawBossWaveBanner();
                }
            }
        }

        private void DrawBossHealthBar()
        {
            if (whiteTexture == null || activeBosses.Count == 0)
            {
                return;
            }

            float totalCurrent = 0f;
            float totalMax = 0f;
            for (var i = 0; i < activeBosses.Count; i++)
            {
                if (activeBosses[i] == null)
                {
                    continue;
                }

                totalCurrent += activeBosses[i].CurrentHealth;
                totalMax += activeBosses[i].MaxHealth;
            }

            if (totalMax <= 0f)
            {
                return;
            }

            const float width = 420f;
            const float height = 10f;
            var x = (Screen.width - width) * 0.5f;
            var y = Screen.height - 34f;
            var progress = Mathf.Clamp01(totalCurrent / totalMax);

            var previous = GUI.color;
            GUI.color = new Color(0.08f, 0.10f, 0.10f, 0.92f);
            GUI.DrawTexture(new Rect(x, y, width, height), whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = new Color(currentBossTint.r, currentBossTint.g, currentBossTint.b, 0.96f);
            GUI.DrawTexture(new Rect(x, y, width * progress, height), whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = previous;

            var label = activeBosses.Count == 1
                ? $"{currentBossDisplayName}  {Mathf.CeilToInt(totalCurrent)} / {Mathf.CeilToInt(totalMax)}"
                : $"{currentBossDisplayName} x{activeBosses.Count}  {Mathf.CeilToInt(totalCurrent)} / {Mathf.CeilToInt(totalMax)}";
            GUI.Label(new Rect(x, y - 18f, width, 18f), label);
        }

        private void DrawBossWaveBanner()
        {
            if (bossWaveBannerTimer <= 0f)
            {
                return;
            }

            var alpha = Mathf.Clamp01(bossWaveBannerTimer / 3.2f);
            var previous = GUI.color;
            GUI.color = new Color(0.92f, 0.98f, 0.94f, alpha);
            GUI.Label(new Rect((Screen.width - 360f) * 0.5f, 48f, 360f, 24f), $"Boss Wave: {currentBossDisplayName}");
            GUI.color = previous;
        }

        private void DrawEndScreen()
        {
            var previousMatrix = GUI.matrix;
            var previousColor = GUI.color;
            var previousBackground = GUI.backgroundColor;

            var scale = Mathf.Max(1.1f, Mathf.Min(Screen.width / 1600f, Screen.height / 900f) * 1.15f);
            var width = 520f;
            var height = 310f;
            var scaledWidth = Screen.width / scale;
            var scaledHeight = Screen.height / scale;
            var box = new Rect((scaledWidth - width) * 0.5f, (scaledHeight - height) * 0.5f, width, height);

            GUI.color = new Color(0.02f, 0.04f, 0.04f, 0.54f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture, ScaleMode.StretchToFill, false);

            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            DrawPanel(box);

            GUI.Label(new Rect(box.x + 36f, box.y + 28f, box.width - 72f, 34f), playerWon ? "Level Cleared" : "Run Failed", endTitleStyle);
            GUI.Label(new Rect(box.x + 36f, box.y + 74f, box.width - 72f, 24f), $"Time: {runTimer:0.0}s", endBodyStyle);
            GUI.Label(
                new Rect(box.x + 48f, box.y + 106f, box.width - 96f, 48f),
                playerWon ? "The mire falls silent. The path forward opens." : "The black mire claims the sailor.",
                endBodyStyle);

            if (playerWon)
            {
                if (!string.IsNullOrWhiteSpace(nextSceneName))
                {
                    GUI.backgroundColor = new Color(0.16f, 0.28f, 0.22f, 0.92f);
                    if (GUI.Button(new Rect(box.x + 76f, box.y + 184f, box.width - 152f, 50f), "Next Level", endButtonStyle))
                    {
                        LoadScene(nextSceneName);
                    }
                }
                else
                {
                    GUI.Label(new Rect(box.x + 36f, box.y + 194f, box.width - 72f, 24f), "Run complete", endBodyStyle);
                }
            }
            else
            {
                GUI.backgroundColor = new Color(0.16f, 0.28f, 0.22f, 0.92f);
                if (GUI.Button(new Rect(box.x + 76f, box.y + 184f, box.width - 152f, 50f), "Retry", endButtonStyle))
                {
                    LoadScene(SceneManager.GetActiveScene().name);
                }
            }

            GUI.backgroundColor = new Color(0.14f, 0.22f, 0.20f, 0.9f);
            if (GUI.Button(new Rect(box.x + 76f, box.y + 246f, box.width - 152f, 50f), "Main Menu", endButtonStyle))
            {
                LoadScene(menuSceneName);
            }

            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
        }

        private void DrawPauseMenu()
        {
            var previousMatrix = GUI.matrix;
            var previousColor = GUI.color;
            var previousBackground = GUI.backgroundColor;

            var scale = Mathf.Max(1.05f, Mathf.Min(Screen.width / 1600f, Screen.height / 900f) * 1.1f);
            var width = 520f;
            var height = 340f;
            var scaledWidth = Screen.width / scale;
            var scaledHeight = Screen.height / scale;
            var box = new Rect((scaledWidth - width) * 0.5f, (scaledHeight - height) * 0.5f, width, height);

            GUI.color = new Color(0.02f, 0.04f, 0.04f, 0.46f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture, ScaleMode.StretchToFill, false);

            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            DrawPanel(box);

            GUI.Label(new Rect(box.x + 36f, box.y + 28f, box.width - 72f, 34f), "Paused", endTitleStyle);
            GUI.Label(new Rect(box.x + 48f, box.y + 74f, box.width - 96f, 42f), "The run is on hold. Resume, restart, or return to the menu.", endBodyStyle);

            GUI.backgroundColor = new Color(0.16f, 0.28f, 0.22f, 0.92f);
            if (GUI.Button(new Rect(box.x + 76f, box.y + 138f, box.width - 152f, 50f), "Resume", endButtonStyle))
            {
                ClosePauseMenu();
            }

            GUI.backgroundColor = new Color(0.18f, 0.24f, 0.20f, 0.92f);
            if (GUI.Button(new Rect(box.x + 76f, box.y + 200f, box.width - 152f, 50f), "Restart", endButtonStyle))
            {
                pauseMenuOpen = false;
                LoadScene(SceneManager.GetActiveScene().name);
            }

            GUI.backgroundColor = new Color(0.14f, 0.22f, 0.20f, 0.9f);
            if (GUI.Button(new Rect(box.x + 76f, box.y + 262f, box.width - 152f, 50f), "Main Menu", endButtonStyle))
            {
                pauseMenuOpen = false;
                LoadScene(menuSceneName);
            }

            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
        }

        private bool IsUpgradeOverlayOpen()
        {
            if (experienceController != null && experienceController.HasPendingChoice)
            {
                return true;
            }

            var corruptionEffects = player != null ? player.GetComponent<CorruptionRuntimeEffects>() : null;
            return corruptionEffects != null && corruptionEffects.HasPendingChoice;
        }

        private void TogglePauseMenu()
        {
            if (pauseMenuOpen)
            {
                ClosePauseMenu();
                return;
            }

            pauseMenuOpen = true;
            Time.timeScale = 0f;
        }

        private void ClosePauseMenu()
        {
            pauseMenuOpen = false;
            Time.timeScale = 1f;
        }

        private static void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        private void EnsureStyles()
        {
            if (endTitleStyle != null)
            {
                return;
            }

            endTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 30,
                fontStyle = FontStyle.Bold
            };
            endTitleStyle.normal.textColor = new Color(0.90f, 0.97f, 0.85f, 1f);

            endBodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17,
                wordWrap = true
            };
            endBodyStyle.normal.textColor = new Color(0.78f, 0.86f, 0.76f, 1f);

            endButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(16, 16, 12, 12)
            };
            endButtonStyle.normal.textColor = new Color(0.93f, 0.98f, 0.92f, 1f);
            endButtonStyle.hover.textColor = Color.white;
            endButtonStyle.active.textColor = Color.white;
        }

        private void DrawPanel(Rect rect)
        {
            if (whiteTexture == null)
            {
                return;
            }

            GUI.color = new Color(0.04f, 0.08f, 0.07f, 0.82f);
            GUI.DrawTexture(rect, whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = new Color(0.34f, 0.52f, 0.36f, 0.95f);
            GUI.DrawTexture(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, 5f), whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = new Color(1f, 1f, 1f, 0.06f);
            GUI.DrawTexture(new Rect(rect.x + 16f, rect.y + 16f, rect.width - 32f, rect.height - 32f), whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;
        }
    }
}
