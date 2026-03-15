using Dagon.Core;
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
        [SerializeField] private HarpoonProjectile bossProjectilePrefab;
        [SerializeField] private float bossSpawnTime = 80f;

        private Health playerHealth;
        private Health bossHealth;
        private float runTimer;
        private bool bossSpawned;
        private bool runEnded;
        private bool playerWon;

        public float RunTimer => runTimer;
        public bool BossSpawned => bossSpawned;
        public bool RunEnded => runEnded;

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
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.Died -= HandlePlayerDied;
            }

            if (bossHealth != null)
            {
                bossHealth.Died -= HandleBossDied;
            }
        }

        private void Update()
        {
            if (runEnded)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                return;
            }

            if (Time.timeScale <= 0f)
            {
                return;
            }

            runTimer += Time.deltaTime;
            if (!bossSpawned && runTimer >= bossSpawnTime)
            {
                SpawnBoss();
            }
        }

        public void Configure(Transform playerTransform, Camera cameraReference, SpawnDirector director, Sprite enemySprite, HarpoonProjectile projectilePrefab)
        {
            player = playerTransform;
            worldCamera = cameraReference;
            spawnDirector = director;
            bossProjectilePrefab = projectilePrefab;
        }

        public void ConfigureStage(float newBossSpawnTime)
        {
            bossSpawnTime = Mathf.Max(0.1f, newBossSpawnTime);
        }

        private void SpawnBoss()
        {
            if (player == null || worldCamera == null)
            {
                return;
            }

            var bossSprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Bosses/mire_colossus", 256f);
            if (bossSprite == null)
            {
                return;
            }

            bossSpawned = true;
            spawnDirector?.StopSpawning();

            var boss = new GameObject("MireColossus");
            boss.transform.SetParent(transform);
            boss.transform.position = player.position + new Vector3(0f, 0.6f, 12f);

            var collider = boss.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1.1f, 0f);
            collider.height = 2.8f;
            collider.radius = 0.9f;
            collider.isTrigger = true;

            var rigidbody = boss.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            bossHealth = boss.AddComponent<Health>();
            bossHealth.SetMaxHealth(120f, true);
            bossHealth.Died += HandleBossDied;

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
            EndRun(true);
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
                GUI.Box(new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - 80f, 360f, 160f), playerWon ? "Run Cleared" : "Run Failed");
                GUI.Label(new Rect(Screen.width * 0.5f - 150f, Screen.height * 0.5f - 25f, 300f, 24f), $"Time: {runTimer:0.0}s");
                GUI.Label(new Rect(Screen.width * 0.5f - 150f, Screen.height * 0.5f + 5f, 300f, 24f), playerWon ? "The Mire Colossus sinks back into the mire." : "The black mire claims the sailor.");
                GUI.Label(new Rect(Screen.width * 0.5f - 150f, Screen.height * 0.5f + 40f, 300f, 24f), "Press R to restart");
                return;
            }

            GUI.Label(new Rect(Screen.width - 220f, 18f, 200f, 22f), $"Run: {runTimer:0.0}s");
            if (!bossSpawned)
            {
                GUI.Label(new Rect(Screen.width - 220f, 40f, 200f, 22f), $"Boss in: {Mathf.Max(0f, bossSpawnTime - runTimer):0.0}s");
            }
            else
            {
                GUI.Label(new Rect(Screen.width - 220f, 40f, 200f, 22f), "Boss active");
            }
        }
    }
}
