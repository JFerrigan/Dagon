using System.Collections.Generic;
using Dagon.Core;
using Dagon.Rendering;
using Dagon.UI;
using UnityEngine;

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
            DeepSpawn
        }

        [SerializeField] private Transform player;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Sprite mireSprite;
        [SerializeField] private Sprite acolyteSprite;
        [SerializeField] private Sprite deepSpawnSprite;
        [SerializeField] private GameObject deepSpawnPrefab;
        [SerializeField] private DrownedAcolyteProjectile acolyteProjectilePrefab;
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private float despawnRadius = 28f;
        [SerializeField] private float minSpawnInterval = 0.5f;
        [SerializeField] private float maxSpawnInterval = 1.2f;
        [SerializeField] private int maxAliveEnemies = 24;
        [SerializeField] private int startingEnemies = 6;
        [SerializeField] private int eliteSpawnEvery = 14;
        [SerializeField] private int regularSpawnQuota = 30;

        private float spawnTimer;
        private int defeatedEnemies;
        private int totalSpawned;
        private bool spawningStopped;
        private bool quotaNotified;
        private readonly HashSet<GameObject> activeEnemies = new();

        public event System.Action SpawnQuotaCompleted;
        public event System.Action BattlefieldCleared;

        public int AliveEnemies => activeEnemies.Count;
        public int DefeatedEnemies => defeatedEnemies;
        public int TotalSpawned => totalSpawned;
        public int RemainingSpawns => Mathf.Max(0, regularSpawnQuota - totalSpawned);
        public bool SpawnQuotaMet => totalSpawned >= regularSpawnQuota;
        public bool IsBattlefieldClear => SpawnQuotaMet && activeEnemies.Count <= 0;

        private void Start()
        {
            SpawnOpeningWave();
            ResetTimer();
        }

        private void Update()
        {
            if (player == null || worldCamera == null)
            {
                return;
            }

            DespawnFarEnemies();

            if (spawningStopped || SpawnQuotaMet || activeEnemies.Count >= maxAliveEnemies)
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
            deepSpawnSprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Enemies/deep_spawn", 256f);
            deepSpawnPrefab = Resources.Load<GameObject>(DeepSpawnPrefabResourcePath);
            acolyteProjectilePrefab = RuntimeAcolyteProjectileFactory.Create(cameraReference);
        }

        public void ConfigureCampaign(int newRegularSpawnQuota, int newStartingEnemies, int newMaxAliveEnemies, int newEliteSpawnEvery, float newMinSpawnInterval, float newMaxSpawnInterval)
        {
            regularSpawnQuota = Mathf.Max(0, newRegularSpawnQuota);
            startingEnemies = Mathf.Max(0, newStartingEnemies);
            maxAliveEnemies = Mathf.Max(1, newMaxAliveEnemies);
            eliteSpawnEvery = Mathf.Max(1, newEliteSpawnEvery);
            minSpawnInterval = Mathf.Max(0.1f, newMinSpawnInterval);
            maxSpawnInterval = Mathf.Max(minSpawnInterval + 0.05f, newMaxSpawnInterval);
            spawningStopped = false;
            quotaNotified = false;
        }

        public void StopSpawning()
        {
            spawningStopped = true;
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
            _ = additionalAliveCap;
            minSpawnInterval = Mathf.Max(0.15f, minSpawnInterval - intervalReduction);
            maxSpawnInterval = Mathf.Max(minSpawnInterval + 0.05f, maxSpawnInterval - intervalReduction);
        }

        private void SpawnOpeningWave()
        {
            var count = Mathf.Min(startingEnemies, Mathf.Min(regularSpawnQuota - totalSpawned, maxAliveEnemies - activeEnemies.Count));
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
            if (SpawnQuotaMet || totalSpawned >= regularSpawnQuota || activeEnemies.Count >= maxAliveEnemies)
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

        private bool TrySpawnSpecificEnemy(EnemyKind enemyKind, Vector3 position)
        {
            if (player == null || mireSprite == null || SpawnQuotaMet || totalSpawned >= regularSpawnQuota || activeEnemies.Count >= maxAliveEnemies)
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
            collider.center = new Vector3(0f, 0.75f, 0f);
            collider.height = 1.5f;
            collider.radius = 0.35f;
            collider.isTrigger = true;

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
            renderer.sortingOrder = enemyKind == EnemyKind.MireWretch ? 5 : enemyKind == EnemyKind.DrownedAcolyte ? 6 : 7;
            renderer.color = Color.white;

            if (enemyKind == EnemyKind.DeepSpawn)
            {
                visuals.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
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

            deepSpawn.Configure(player, worldCamera);

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
                EnemyKind.MireWretch => new Vector3(0f, 1.28f, 0f),
                EnemyKind.DrownedAcolyte => new Vector3(0f, 1.4f, 0f),
                _ => new Vector3(0f, 1.68f, 0f)
            };
            bar.Configure(worldCamera, offset, false);
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
            spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
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
            if (eliteSpawnEvery > 0 &&
                nextSpawnIndex > 1 &&
                nextSpawnIndex % eliteSpawnEvery == 0 &&
                deepSpawnPrefab != null)
            {
                return EnemyKind.DeepSpawn;
            }

            if (nextSpawnIndex % 5 == 0 && acolyteProjectilePrefab != null)
            {
                return EnemyKind.DrownedAcolyte;
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
            return player.position + new Vector3(direction2D.x, 0.5f, direction2D.y) * spawnRadius;
        }

        private static float GetMaxHealth(EnemyKind enemyKind)
        {
            return enemyKind switch
            {
                EnemyKind.MireWretch => 3f,
                EnemyKind.DrownedAcolyte => 5f,
                EnemyKind.DeepSpawn => 24f,
                _ => 3f
            };
        }

        private Sprite GetSprite(EnemyKind enemyKind)
        {
            return enemyKind switch
            {
                EnemyKind.DrownedAcolyte when acolyteSprite != null => acolyteSprite,
                EnemyKind.DeepSpawn when deepSpawnSprite != null => deepSpawnSprite,
                _ => mireSprite
            };
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
