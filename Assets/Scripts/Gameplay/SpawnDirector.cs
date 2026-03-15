using Dagon.Core;
using Dagon.Rendering;
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
        [SerializeField] private HarpoonProjectile acolyteProjectilePrefab;
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private float minSpawnInterval = 0.5f;
        [SerializeField] private float maxSpawnInterval = 1.2f;
        [SerializeField] private int maxAliveEnemies = 24;
        [SerializeField] private int startingEnemies = 6;
        [SerializeField] private int eliteSpawnEvery = 14;

        private float spawnTimer;
        private int aliveEnemies;
        private int totalSpawned;

        private void Start()
        {
            for (var i = 0; i < startingEnemies; i++)
            {
                SpawnEnemy();
            }

            ResetTimer();
        }

        private void Update()
        {
            if (player == null || worldCamera == null)
            {
                return;
            }

            if (aliveEnemies >= maxAliveEnemies)
            {
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
            {
                return;
            }

            SpawnEnemy();
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
            acolyteProjectilePrefab = rangedProjectilePrefab;
        }

        private void SpawnEnemy()
        {
            if (player == null || mireSprite == null)
            {
                return;
            }

            var ring = Random.insideUnitCircle.normalized * Random.Range(spawnRadius * 0.75f, spawnRadius);
            var position = player.position + new Vector3(ring.x, 0.5f, ring.y);
            var enemyKind = ChooseEnemyKind();

            if (enemyKind == EnemyKind.DeepSpawn && TrySpawnDeepSpawn(position))
            {
                return;
            }

            var mire = new GameObject($"{enemyKind}_{aliveEnemies + 1}");
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

            var rewards = mire.AddComponent<EnemyDeathRewards>();
            if (enemyKind == EnemyKind.MireWretch)
            {
                var contactDamage = mire.AddComponent<ContactDamage>();
                contactDamage.Configure(1f);

                var wanderer = mire.AddComponent<MireWanderer>();
                wanderer.Configure(player, Random.Range(1.3f, 1.7f), 3f, 8f);
                rewards.Configure(1, 1.5f);
            }
            else
            {
                if (enemyKind == EnemyKind.DrownedAcolyte)
                {
                    var shooter = mire.AddComponent<DrownedAcolyteShooter>();
                    shooter.Configure(player, acolyteProjectilePrefab, 1.1f, 5.5f, 2.2f);
                    rewards.Configure(3, 3f);
                }
                else
                {
                    var bruiser = mire.AddComponent<DeepSpawnBruiser>();
                    bruiser.Configure(player, 1.15f, 4.4f);

                    var contactDamage = mire.AddComponent<ContactDamage>();
                    contactDamage.Configure(3f);
                    rewards.Configure(6, 7f);
                }
            }

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(mire.transform, false);
            visuals.transform.localPosition = Vector3.zero;

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSprite(enemyKind);
            renderer.sortingOrder = enemyKind == EnemyKind.MireWretch ? 5 : enemyKind == EnemyKind.DrownedAcolyte ? 6 : 7;
            renderer.color = GetTint(enemyKind);

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

            aliveEnemies += 1;
            totalSpawned += 1;
        }

        private bool TrySpawnDeepSpawn(Vector3 position)
        {
            if (deepSpawnPrefab == null)
            {
                return false;
            }

            var deepSpawnObject = Instantiate(deepSpawnPrefab, position, Quaternion.identity, transform);
            deepSpawnObject.name = $"{EnemyKind.DeepSpawn}_{aliveEnemies + 1}";

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
            aliveEnemies += 1;
            totalSpawned += 1;
            return true;
        }

        private void HandleEnemyDied(Health health, GameObject source)
        {
            health.Died -= HandleEnemyDied;
            aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
        }

        private void ResetTimer()
        {
            spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
        }

        public void TightenPressure(float intervalReduction, int additionalAliveCap)
        {
            minSpawnInterval = Mathf.Max(0.15f, minSpawnInterval - intervalReduction);
            maxSpawnInterval = Mathf.Max(minSpawnInterval + 0.05f, maxSpawnInterval - intervalReduction);
            maxAliveEnemies = Mathf.Max(maxAliveEnemies, maxAliveEnemies + additionalAliveCap);
        }

        public void StopSpawning()
        {
            enabled = false;
        }

        public void ConfigureStage(int startingEnemyCount, bool disableContinuousSpawning)
        {
            startingEnemies = Mathf.Max(0, startingEnemyCount);
            if (disableContinuousSpawning)
            {
                enabled = false;
            }
        }

        private EnemyKind ChooseEnemyKind()
        {
            if (totalSpawned > 0 && totalSpawned % eliteSpawnEvery == 0)
            {
                return EnemyKind.DeepSpawn;
            }

            if (acolyteProjectilePrefab == null)
            {
                return EnemyKind.MireWretch;
            }

            return Random.value < 0.2f ? EnemyKind.DrownedAcolyte : EnemyKind.MireWretch;
        }

        private static float GetMaxHealth(EnemyKind enemyKind)
        {
            return enemyKind switch
            {
                EnemyKind.MireWretch => 3f,
                EnemyKind.DrownedAcolyte => 7f,
                EnemyKind.DeepSpawn => 20f,
                _ => 3f
            };
        }

        private static Color GetTint(EnemyKind enemyKind)
        {
            return enemyKind switch
            {
                EnemyKind.MireWretch => Color.white,
                EnemyKind.DrownedAcolyte => Color.white,
                EnemyKind.DeepSpawn => Color.white,
                _ => Color.white
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
    }
}
