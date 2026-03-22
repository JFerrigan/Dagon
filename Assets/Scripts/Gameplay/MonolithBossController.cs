using System.Collections.Generic;
using Dagon.Core;
using Dagon.Rendering;
using Dagon.UI;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MonolithBossController : MonoBehaviour
    {
        private const string WideLeechSpritePath = "Sprites/Enemies/wide_leech";
        private const string TallLeechSpritePath = "Sprites/Enemies/tall_leech";

        [SerializeField] private Transform target;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private HarpoonProjectile projectilePrefab;
        [SerializeField] private float summonRadius = 5.2f;
        [SerializeField] private float wideSummonCooldown = 2.4f;
        [SerializeField] private float tallSummonCooldown = 4.9f;
        [SerializeField] private int maxWideLeeches = 6;
        [SerializeField] private int maxTallLeeches = 3;

        private readonly List<GameObject> activeWideLeeches = new();
        private readonly List<GameObject> activeTallLeeches = new();
        private Sprite wideLeechSprite;
        private Sprite tallLeechSprite;
        private float wideSummonTimer;
        private float tallSummonTimer;

        private void Awake()
        {
            wideLeechSprite = RuntimeSpriteLibrary.LoadSprite(WideLeechSpritePath, 64f);
            tallLeechSprite = RuntimeSpriteLibrary.LoadSprite(TallLeechSpritePath, 64f);
        }

        private void Update()
        {
            ResolveReferences();
            CleanupDestroyedLeeches(activeWideLeeches);
            CleanupDestroyedLeeches(activeTallLeeches);

            wideSummonTimer -= Time.deltaTime;
            tallSummonTimer -= Time.deltaTime;

            if (activeWideLeeches.Count < maxWideLeeches && wideSummonTimer <= 0f)
            {
                SpawnWideLeech();
                wideSummonTimer = wideSummonCooldown;
            }

            if (activeTallLeeches.Count < maxTallLeeches && tallSummonTimer <= 0f)
            {
                SpawnTallLeech();
                tallSummonTimer = tallSummonCooldown;
            }
        }

        public void Configure(
            Transform newTarget,
            Camera cameraReference,
            HarpoonProjectile leechProjectilePrefab,
            float newWideSummonCooldown,
            float newTallSummonCooldown,
            int newMaxWideLeeches,
            int newMaxTallLeeches)
        {
            target = newTarget;
            worldCamera = cameraReference;
            projectilePrefab = leechProjectilePrefab;
            wideSummonCooldown = Mathf.Max(0.2f, newWideSummonCooldown);
            tallSummonCooldown = Mathf.Max(0.2f, newTallSummonCooldown);
            maxWideLeeches = Mathf.Max(1, newMaxWideLeeches);
            maxTallLeeches = Mathf.Max(1, newMaxTallLeeches);
            wideSummonTimer = 0.9f;
            tallSummonTimer = 1.8f;
        }

        private void ResolveReferences()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                target = player != null ? player.transform : null;
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (projectilePrefab == null && worldCamera != null)
            {
                projectilePrefab = RuntimeOrbProjectileFactory.Create(worldCamera);
            }
        }

        private void SpawnWideLeech()
        {
            if (wideLeechSprite == null)
            {
                return;
            }

            var leech = CreateBaseSummon("WideLeech", BuildSummonPosition(), 4f, new Vector3(0f, 0.21f, 0f), 0.23f, 0.45f, wideLeechSprite, new Vector3(0.85f, 0.85f, 1f), new Vector3(0f, 0.04f, 0f), new Vector3(0f, 0.52f, 0f));
            var contactDamage = leech.AddComponent<ContactDamage>();
            contactDamage.Configure(1f);
            var chaser = leech.AddComponent<SimpleEnemyChaser>();
            chaser.Configure(4.1f, 0.32f);
            var rewards = leech.AddComponent<EnemyDeathRewards>();
            rewards.Configure(0, 0f);
            activeWideLeeches.Add(leech);
        }

        private void SpawnTallLeech()
        {
            if (tallLeechSprite == null || projectilePrefab == null)
            {
                return;
            }

            var leech = CreateBaseSummon("TallLeech", BuildSummonPosition(), 5f, new Vector3(0f, 0.95f, 0f), 0.4f, 1.9f, tallLeechSprite, new Vector3(1.45f, 1.45f, 1f), Vector3.zero, new Vector3(0f, 1.8f, 0f));
            var shooter = leech.AddComponent<TallLeechShooter>();
            shooter.Configure(target, projectilePrefab, worldCamera, 9f, 2.1f, 7.4f, 1f);
            var rewards = leech.AddComponent<EnemyDeathRewards>();
            rewards.Configure(0, 0f);
            activeTallLeeches.Add(leech);
        }

        private GameObject CreateBaseSummon(
            string objectName,
            Vector3 position,
            float maxHealth,
            Vector3 colliderCenter,
            float colliderRadius,
            float colliderHeight,
            Sprite sprite,
            Vector3 visualScale,
            Vector3 visualOffset,
            Vector3 healthBarOffset)
        {
            var summon = new GameObject(objectName);
            summon.transform.position = position;
            summon.transform.SetParent(transform.parent, true);

            var collider = summon.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            collider.center = colliderCenter;
            collider.radius = colliderRadius;
            collider.height = colliderHeight;

            var rigidbody = summon.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            var health = summon.AddComponent<Health>();
            health.SetMaxHealth(maxHealth, true);
            summon.AddComponent<Hurtbox>().Configure(CombatTeam.Enemy, health);
            summon.AddComponent<KnockbackReceiver>().Configure(0.8f, 18f, 4.8f);

            var healthBar = summon.AddComponent<EnemyHealthBar>();
            healthBar.Configure(worldCamera, healthBarOffset, true, 2.25f);

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(summon.transform, false);
            visuals.transform.localPosition = visualOffset;
            visuals.transform.localScale = visualScale;

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 12;
            renderer.color = Color.white;

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);

            return summon;
        }

        private Vector3 BuildSummonPosition()
        {
            var direction = Random.insideUnitCircle;
            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector2.right;
            }

            direction.Normalize();
            var position = transform.position + new Vector3(direction.x, 0f, direction.y) * summonRadius;
            position.y = target != null ? target.position.y : 0f;
            return position;
        }

        private static void CleanupDestroyedLeeches(List<GameObject> leeches)
        {
            for (var index = leeches.Count - 1; index >= 0; index--)
            {
                if (leeches[index] == null)
                {
                    leeches.RemoveAt(index);
                }
            }
        }
    }
}
