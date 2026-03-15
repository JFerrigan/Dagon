using System.Collections.Generic;
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

        private enum SpawnPattern
        {
            SurroundRing,
            FrontCone,
            FlankPincer,
            EliteEscort
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
        private int aliveEnemies;
        private int defeatedEnemies;
        private int totalSpawned;
        private bool spawningStopped;
        private bool quotaNotified;

        public event System.Action SpawnQuotaCompleted;
        public event System.Action BattlefieldCleared;

        public int AliveEnemies => aliveEnemies;
        public int DefeatedEnemies => defeatedEnemies;
        public int TotalSpawned => totalSpawned;
        public int RemainingSpawns => Mathf.Max(0, regularSpawnQuota - defeatedEnemies);
        public bool SpawnQuotaMet => defeatedEnemies >= regularSpawnQuota;
        public bool IsBattlefieldClear => SpawnQuotaMet && aliveEnemies <= 0;

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

            if (spawningStopped || SpawnQuotaMet || aliveEnemies >= maxAliveEnemies)
            {
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
            {
                return;
            }

            if (TrySpawnEncounter())
            {
                ResetTimer();
            }
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
            minSpawnInterval = Mathf.Max(0.15f, minSpawnInterval - intervalReduction);
            maxSpawnInterval = Mathf.Max(minSpawnInterval + 0.05f, maxSpawnInterval - intervalReduction);
            maxAliveEnemies = Mathf.Max(maxAliveEnemies, maxAliveEnemies + additionalAliveCap);
        }

        private void SpawnOpeningWave()
        {
            var count = Mathf.Min(startingEnemies, regularSpawnQuota);
            var positions = BuildPatternPositions(SpawnPattern.SurroundRing, count);
            for (var i = 0; i < positions.Count; i++)
            {
                if (!TrySpawnSpecificEnemy(EnemyKind.MireWretch, positions[i]))
                {
                    break;
                }
            }
        }

        private bool TrySpawnEncounter()
        {
            if (SpawnQuotaMet || defeatedEnemies + aliveEnemies >= regularSpawnQuota)
            {
                NotifyQuotaCompletedIfNeeded();
                return false;
            }

            var pattern = ChooseSpawnPattern();
            var spawnedAny = pattern == SpawnPattern.EliteEscort
                ? SpawnEliteEscort()
                : SpawnPatternWave(pattern);

            if (SpawnQuotaMet)
            {
                NotifyQuotaCompletedIfNeeded();
            }

            return spawnedAny;
        }

        private bool SpawnPatternWave(SpawnPattern pattern)
        {
            var phase = GetPhase();
            var count = phase switch
            {
                0 => 2,
                1 => 3,
                _ => 4
            };

            var positions = BuildPatternPositions(pattern, count);
            var spawned = false;
            for (var i = 0; i < positions.Count; i++)
            {
                var kind = ChooseStandardEnemyKind(phase, i);
                if (TrySpawnSpecificEnemy(kind, positions[i]))
                {
                    spawned = true;
                }
            }

            return spawned;
        }

        private bool SpawnEliteEscort()
        {
            var positions = BuildPatternPositions(SpawnPattern.EliteEscort, 4);
            var spawned = false;
            if (positions.Count > 0)
            {
                spawned |= TrySpawnSpecificEnemy(EnemyKind.DeepSpawn, positions[0]);
            }

            for (var i = 1; i < positions.Count; i++)
            {
                var kind = i == positions.Count - 1 ? EnemyKind.DrownedAcolyte : EnemyKind.MireWretch;
                spawned |= TrySpawnSpecificEnemy(kind, positions[i]);
            }

            return spawned;
        }

        private bool TrySpawnSpecificEnemy(EnemyKind enemyKind, Vector3 position)
        {
            if (player == null || mireSprite == null || SpawnQuotaMet || defeatedEnemies + aliveEnemies >= regularSpawnQuota || aliveEnemies >= maxAliveEnemies)
            {
                return false;
            }

            if (enemyKind == EnemyKind.DeepSpawn && TrySpawnDeepSpawn(position))
            {
                return true;
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
            switch (enemyKind)
            {
                case EnemyKind.MireWretch:
                {
                    var contactDamage = mire.AddComponent<ContactDamage>();
                    contactDamage.Configure(1f);

                    var wanderer = mire.AddComponent<MireWanderer>();
                    wanderer.Configure(player, Random.Range(3.2f, 3.6f), 3f, 18f);
                    rewards.Configure(1, 1.5f);
                    break;
                }
                case EnemyKind.DrownedAcolyte:
                {
                    var shooter = mire.AddComponent<DrownedAcolyteShooter>();
                    shooter.Configure(player, acolyteProjectilePrefab, Random.Range(2.4f, 2.8f), 6f, 1.6f, worldCamera);
                    rewards.Configure(3, 3f);
                    break;
                }
                case EnemyKind.DeepSpawn:
                default:
                {
                    var bruiser = mire.AddComponent<DeepSpawnBruiser>();
                    bruiser.Configure(player, 1.2f, 4.8f);
                    var contactDamage = mire.AddComponent<ContactDamage>();
                    contactDamage.Configure(3f);
                    rewards.Configure(6, 7f);
                    break;
                }
            }

            ConfigureVisuals(mire.transform, enemyKind);
            aliveEnemies += 1;
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
            if (player == null || aliveEnemies <= 0)
            {
                return;
            }

            var despawnRadiusSquared = despawnRadius * despawnRadius;
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var enemy = transform.GetChild(i);
                var offset = enemy.position - player.position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= despawnRadiusSquared)
                {
                    continue;
                }

                DespawnEnemy(enemy.gameObject);
            }
        }

        private void DespawnEnemy(GameObject enemy)
        {
            if (enemy == null)
            {
                return;
            }

            var health = enemy.GetComponent<Health>();
            if (health != null)
            {
                health.Died -= HandleEnemyDied;
            }

            aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
            Destroy(enemy);
        }

        private SpawnPattern ChooseSpawnPattern()
        {
            if (totalSpawned > 0 && totalSpawned % eliteSpawnEvery == 0)
            {
                return SpawnPattern.EliteEscort;
            }

            var phase = GetPhase();
            var roll = Random.value;
            return phase switch
            {
                0 => roll < 0.55f ? SpawnPattern.SurroundRing : SpawnPattern.FrontCone,
                1 => roll < 0.35f ? SpawnPattern.SurroundRing : roll < 0.7f ? SpawnPattern.FrontCone : SpawnPattern.FlankPincer,
                _ => roll < 0.25f ? SpawnPattern.SurroundRing : roll < 0.5f ? SpawnPattern.FrontCone : roll < 0.8f ? SpawnPattern.FlankPincer : SpawnPattern.EliteEscort
            };
        }

        private int GetPhase()
        {
            if (defeatedEnemies < regularSpawnQuota * 0.33f)
            {
                return 0;
            }

            if (defeatedEnemies < regularSpawnQuota * 0.72f)
            {
                return 1;
            }

            return 2;
        }

        private EnemyKind ChooseStandardEnemyKind(int phase, int index)
        {
            if (phase == 0)
            {
                return index == 0 && Random.value < 0.18f ? EnemyKind.DrownedAcolyte : EnemyKind.MireWretch;
            }

            if (phase == 1)
            {
                return index == 0 && Random.value < 0.35f ? EnemyKind.DrownedAcolyte : Random.value < 0.22f ? EnemyKind.DrownedAcolyte : EnemyKind.MireWretch;
            }

            return index == 0 || Random.value < 0.3f ? EnemyKind.DrownedAcolyte : EnemyKind.MireWretch;
        }

        private List<Vector3> BuildPatternPositions(SpawnPattern pattern, int count)
        {
            var positions = new List<Vector3>(count);
            var forward = ResolvePatternForward();
            var right = new Vector3(forward.z, 0f, -forward.x);
            switch (pattern)
            {
                case SpawnPattern.SurroundRing:
                    for (var i = 0; i < count; i++)
                    {
                        var angle = (360f / Mathf.Max(1, count)) * i + Random.Range(-18f, 18f);
                        var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                        positions.Add(player.position + direction * Random.Range(spawnRadius * 0.78f, spawnRadius) + Vector3.up * 0.5f);
                    }
                    break;
                case SpawnPattern.FrontCone:
                    for (var i = 0; i < count; i++)
                    {
                        var yaw = Random.Range(-26f, 26f);
                        var direction = Quaternion.Euler(0f, yaw, 0f) * forward;
                        positions.Add(player.position + direction * Random.Range(spawnRadius * 0.75f, spawnRadius * 0.96f) + Vector3.up * 0.5f);
                    }
                    break;
                case SpawnPattern.FlankPincer:
                    for (var i = 0; i < count; i++)
                    {
                        var flank = i % 2 == 0 ? right : -right;
                        var mixed = (flank + forward * Random.Range(-0.25f, 0.35f)).normalized;
                        positions.Add(player.position + mixed * Random.Range(spawnRadius * 0.82f, spawnRadius) + Vector3.up * 0.5f);
                    }
                    break;
                case SpawnPattern.EliteEscort:
                    positions.Add(player.position + forward * (spawnRadius * 0.88f) + Vector3.up * 0.5f);
                    positions.Add(player.position + (forward + right * 0.35f).normalized * (spawnRadius * 0.82f) + Vector3.up * 0.5f);
                    positions.Add(player.position + (forward - right * 0.35f).normalized * (spawnRadius * 0.82f) + Vector3.up * 0.5f);
                    positions.Add(player.position + (forward - right * 0.1f).normalized * (spawnRadius * 0.92f) + Vector3.up * 0.5f);
                    break;
            }

            return positions;
        }

        private Vector3 ResolvePatternForward()
        {
            var mover = player != null ? player.GetComponent<PlayerMover>() : null;
            if (mover != null && mover.MoveDirection.sqrMagnitude > 0.05f)
            {
                return mover.MoveDirection.normalized;
            }

            if (mover != null && mover.AimDirection.sqrMagnitude > 0.05f)
            {
                return mover.AimDirection.normalized;
            }

            return Vector3.forward;
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
    }
}
