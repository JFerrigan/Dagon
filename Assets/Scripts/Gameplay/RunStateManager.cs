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
        [SerializeField] private Transform player;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private SpawnDirector spawnDirector;
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
        private string currentBossDisplayName = "Mire Colossus";
        private string currentBiomeDisplayName = "Black Mire";
        private Color currentBossTint = Color.white;
        private string currentBossSpritePath = "Sprites/Bosses/mire_colossus";

        public float RunTimer => runTimer;
        public bool RunEnded => runEnded;
        public bool BossWaveStarted => bossWaveStarted;
        public bool PauseMenuOpen => pauseMenuOpen;
        public event System.Action BossWaveCompleted;

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

        public void ConfigureBiome(RuntimeBiomeProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            currentBossDisplayName = string.IsNullOrWhiteSpace(profile.BossDisplayName) ? currentBossDisplayName : profile.BossDisplayName;
            currentBiomeDisplayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? currentBiomeDisplayName : profile.DisplayName;
            currentBossTint = profile.BossTint;
            currentBossSpritePath = string.IsNullOrWhiteSpace(profile.BossSpritePath) ? currentBossSpritePath : profile.BossSpritePath;
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
                SpawnBoss(i, bossesInWave);
            }
        }

        private void SpawnBoss(int index, int totalBosses)
        {
            var bossSprite = RuntimeSpriteLibrary.LoadSprite(currentBossSpritePath, 256f) ??
                RuntimeSpriteLibrary.LoadSprite("Sprites/Bosses/mire_colossus", 256f);
            if (bossSprite == null)
            {
                return;
            }

            var boss = new GameObject("MireColossus");
            boss.transform.SetParent(transform);
            var angle = totalBosses <= 1 ? 0f : (360f / totalBosses) * index;
            var offset = Quaternion.Euler(0f, angle, 0f) * (Vector3.forward * 12f);
            boss.transform.position = player.position + new Vector3(offset.x, 0.6f, offset.z);

            var collider = boss.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1.15f, 0f);
            collider.height = 3.2f;
            collider.radius = 1.15f;
            collider.isTrigger = true;

            var rigidbody = boss.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            var bossHealth = boss.AddComponent<Health>();
            bossHealth.SetMaxHealth(35f, true);
            bossHealth.Died += HandleBossDied;
            activeBosses.Add(bossHealth);
            boss.AddComponent<Hurtbox>().Configure(CombatTeam.Enemy, bossHealth);
            boss.AddComponent<KnockbackReceiver>().Configure(0.18f, 22f, 2.4f);

            var contactDamage = boss.AddComponent<ContactDamage>();
            contactDamage.Configure(4f);

            var controller = boss.AddComponent<MireColossusController>();
            controller.Configure(player, bossProjectilePrefab);

            var rewards = boss.AddComponent<EnemyDeathRewards>();
            rewards.Configure(20, 20f);

            var healthBar = boss.AddComponent<EnemyHealthBar>();
            healthBar.Configure(worldCamera, new Vector3(0f, 2.45f, 0f), false);

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(boss.transform, false);

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = bossSprite;
            renderer.sortingOrder = 20;
            renderer.color = currentBossTint;
            visuals.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

            var billboard = visuals.AddComponent<Dagon.Rendering.BillboardSprite>();
            billboard.Configure(worldCamera, Dagon.Rendering.BillboardSprite.BillboardMode.YAxisOnly);
        }

        private void HandlePlayerDied(Health health, GameObject source)
        {
            EndRun(false);
        }

        private void HandleBossDied(Health health, GameObject source)
        {
            health.Died -= HandleBossDied;
            activeBosses.Remove(health);
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
            if (!bossWaveStarted)
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
                DrawBossWaveBanner();
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
            return experienceController != null && experienceController.HasPendingChoice;
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
