using System.Collections.Generic;
using Dagon.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RunStateManager : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private SpawnDirector spawnDirector;
        [SerializeField] private HarpoonProjectile bossProjectilePrefab;
        [SerializeField] private string nextSceneName;
        [SerializeField] private string menuSceneName = "MainMenu";
        [SerializeField] private int bossesInWave = 1;

        private readonly List<Health> activeBosses = new();
        private Health playerHealth;
        private float runTimer;
        private bool bossWaveStarted;
        private bool runEnded;
        private bool playerWon;

        public float RunTimer => runTimer;
        public bool RunEnded => runEnded;
        public bool BossWaveStarted => bossWaveStarted;

        private void Start()
        {
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
            if (runEnded)
            {
                return;
            }

            if (Time.timeScale <= 0f)
            {
                return;
            }

            runTimer += Time.deltaTime;
            if (!bossWaveStarted && spawnDirector != null && spawnDirector.IsBattlefieldClear)
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

        private void HandleBattlefieldCleared()
        {
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
            spawnDirector?.StopSpawning();

            for (var i = 0; i < bossesInWave; i++)
            {
                SpawnBoss(i, bossesInWave);
            }
        }

        private void SpawnBoss(int index, int totalBosses)
        {
            var bossSprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Bosses/mire_colossus", 256f);
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
            collider.center = new Vector3(0f, 1.1f, 0f);
            collider.height = 2.8f;
            collider.radius = 0.9f;
            collider.isTrigger = true;

            var rigidbody = boss.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            var bossHealth = boss.AddComponent<Health>();
            bossHealth.SetMaxHealth(120f, true);
            bossHealth.Died += HandleBossDied;
            activeBosses.Add(bossHealth);

            var contactDamage = boss.AddComponent<ContactDamage>();
            contactDamage.Configure(4f);

            var controller = boss.AddComponent<MireColossusController>();
            controller.Configure(player, bossProjectilePrefab);

            var rewards = boss.AddComponent<EnemyDeathRewards>();
            rewards.Configure(20, 20f);

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(boss.transform, false);

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = bossSprite;
            renderer.sortingOrder = 20;
            renderer.color = Color.white;
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
                EndRun(true);
            }
        }

        private void EndRun(bool won)
        {
            if (runEnded)
            {
                return;
            }

            runEnded = true;
            playerWon = won;
            Time.timeScale = 0f;
        }

        private void OnGUI()
        {
            if (runEnded)
            {
                DrawEndScreen();
                return;
            }

            GUI.Label(new Rect(Screen.width - 220f, 18f, 200f, 22f), $"Run: {runTimer:0.0}s");
            if (!bossWaveStarted)
            {
                var enemiesLeft = spawnDirector != null ? spawnDirector.RemainingSpawns : 0;
                GUI.Label(new Rect(Screen.width - 220f, 40f, 200f, 22f), $"Remaining: {enemiesLeft}");
            }
            else
            {
                GUI.Label(new Rect(Screen.width - 220f, 40f, 200f, 22f), $"Bosses left: {activeBosses.Count}");
            }
        }

        private void DrawEndScreen()
        {
            var box = new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - 100f, 360f, 200f);
            GUI.Box(box, playerWon ? "Level Cleared" : "Run Failed");
            GUI.Label(new Rect(box.x + 28f, box.y + 40f, 300f, 24f), $"Time: {runTimer:0.0}s");
            GUI.Label(
                new Rect(box.x + 28f, box.y + 68f, 300f, 24f),
                playerWon ? "The mire falls silent. The path forward opens." : "The black mire claims the sailor.");

            if (playerWon)
            {
                if (!string.IsNullOrWhiteSpace(nextSceneName))
                {
                    if (GUI.Button(new Rect(box.x + 28f, box.y + 110f, 132f, 34f), "Next Level"))
                    {
                        LoadScene(nextSceneName);
                    }
                }
                else
                {
                    GUI.Label(new Rect(box.x + 28f, box.y + 110f, 220f, 24f), "Run complete");
                }
            }
            else
            {
                if (GUI.Button(new Rect(box.x + 28f, box.y + 110f, 132f, 34f), "Retry"))
                {
                    LoadScene(SceneManager.GetActiveScene().name);
                }
            }

            if (GUI.Button(new Rect(box.x + 28f, box.y + 150f, 132f, 34f), "Main Menu"))
            {
                LoadScene(menuSceneName);
            }
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
    }
}
