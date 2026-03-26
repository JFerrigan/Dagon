using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RotBeaconBombProjectile : MonoBehaviour
    {
        private enum LandingPayloadMode
        {
            Beacon,
            HostilePool
        }

        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float travelDuration;
        private float arcHeight;
        private float elapsed;
        private Camera worldCamera;
        private GameObject owner;

        private float pulseRadius;
        private int pulseCount;
        private float pulseInterval;
        private float pulseDamage;
        private float slowAmount;
        private float slowDuration;
        private float explosionRadius;
        private float explosionDamage;
        private LayerMask enemyMask;

        private string beaconSpritePath;
        private Color beaconTint;
        private Vector3 beaconScale;
        private int beaconSortingOrder;
        private string pulseSpritePath;
        private Color pulseTint;
        private string explosionSpritePath;
        private Color explosionTint;
        private int effectSortingOrder;
        private float heightOffset;
        private LandingPayloadMode landingPayloadMode;
        private float poolDuration;
        private float poolTickDamage;
        private float poolTickInterval;
        private float poolSlowAmount;
        private float poolSlowDuration;
        private string poolName = "EnemyThrownPool";

        internal static void Spawn(
            Vector3 start,
            Vector3 target,
            float duration,
            float lobHeight,
            Camera camera,
            GameObject sourceOwner,
            string projectileSpritePath,
            Color projectileTint,
            Vector3 projectileScale,
            int projectileSortingOrder,
            float projectileHeightOffset,
            float pulseRadius,
            int pulseCount,
            float pulseInterval,
            float pulseDamage,
            float slowAmount,
            float slowDuration,
            float explosionRadius,
            float explosionDamage,
            LayerMask enemyMask,
            string beaconSpritePath,
            Color beaconTint,
            Vector3 beaconScale,
            int beaconSortingOrder,
            string pulseSpritePath,
            Color pulseTint,
            string explosionSpritePath,
            Color explosionTint,
            int effectSortingOrder)
        {
            var projectile = new GameObject("RotBeaconBombProjectile");
            var component = projectile.AddComponent<RotBeaconBombProjectile>();
            component.Initialize(
                start,
                target,
                duration,
                lobHeight,
                camera,
                sourceOwner,
                projectileSpritePath,
                projectileTint,
                projectileScale,
                projectileSortingOrder,
                projectileHeightOffset,
                pulseRadius,
                pulseCount,
                pulseInterval,
                pulseDamage,
                slowAmount,
                slowDuration,
                explosionRadius,
                explosionDamage,
                enemyMask,
                beaconSpritePath,
                beaconTint,
                beaconScale,
                beaconSortingOrder,
                pulseSpritePath,
                pulseTint,
                explosionSpritePath,
                explosionTint,
                effectSortingOrder);
        }

        internal static void SpawnHostilePool(
            Vector3 start,
            Vector3 target,
            float duration,
            float lobHeight,
            Camera camera,
            GameObject sourceOwner,
            string projectileSpritePath,
            Color projectileTint,
            Vector3 projectileScale,
            int projectileSortingOrder,
            float projectileHeightOffset,
            float radius,
            float zoneDuration,
            float tickDamage,
            float tickInterval,
            float slowAmount,
            float slowDuration,
            string poolName)
        {
            var projectile = new GameObject("RotBeaconBombProjectile");
            var component = projectile.AddComponent<RotBeaconBombProjectile>();
            component.InitializeHostilePool(
                start,
                target,
                duration,
                lobHeight,
                camera,
                sourceOwner,
                projectileSpritePath,
                projectileTint,
                projectileScale,
                projectileSortingOrder,
                projectileHeightOffset,
                radius,
                zoneDuration,
                tickDamage,
                tickInterval,
                slowAmount,
                slowDuration,
                poolName);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var progress = travelDuration > 0.0001f ? Mathf.Clamp01(elapsed / travelDuration) : 1f;
            var position = Vector3.Lerp(startPosition, targetPosition, progress);
            position.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;
            transform.position = position + (Vector3.up * heightOffset);

            if (progress >= 1f)
            {
                Land();
            }
        }

        private void Initialize(
            Vector3 start,
            Vector3 target,
            float duration,
            float lobHeight,
            Camera camera,
            GameObject sourceOwner,
            string projectileSpritePath,
            Color projectileTint,
            Vector3 projectileScale,
            int projectileSortingOrder,
            float projectileHeightOffset,
            float newPulseRadius,
            int newPulseCount,
            float newPulseInterval,
            float newPulseDamage,
            float newSlowAmount,
            float newSlowDuration,
            float newExplosionRadius,
            float newExplosionDamage,
            LayerMask newEnemyMask,
            string newBeaconSpritePath,
            Color newBeaconTint,
            Vector3 newBeaconScale,
            int newBeaconSortingOrder,
            string newPulseSpritePath,
            Color newPulseTint,
            string newExplosionSpritePath,
            Color newExplosionTint,
            int newEffectSortingOrder)
        {
            startPosition = start;
            targetPosition = target;
            travelDuration = Mathf.Max(0.05f, duration);
            arcHeight = Mathf.Max(0.05f, lobHeight);
            worldCamera = camera;
            owner = sourceOwner;
            heightOffset = projectileHeightOffset;
            landingPayloadMode = LandingPayloadMode.Beacon;

            pulseRadius = Mathf.Max(0.1f, newPulseRadius);
            pulseCount = Mathf.Max(1, newPulseCount);
            pulseInterval = Mathf.Max(0.05f, newPulseInterval);
            pulseDamage = Mathf.Max(0.1f, newPulseDamage);
            slowAmount = Mathf.Clamp01(newSlowAmount);
            slowDuration = Mathf.Max(0f, newSlowDuration);
            explosionRadius = Mathf.Max(0.1f, newExplosionRadius);
            explosionDamage = Mathf.Max(0.1f, newExplosionDamage);
            enemyMask = newEnemyMask;

            beaconSpritePath = newBeaconSpritePath;
            beaconTint = newBeaconTint;
            beaconScale = newBeaconScale;
            beaconSortingOrder = newBeaconSortingOrder;
            pulseSpritePath = newPulseSpritePath;
            pulseTint = newPulseTint;
            explosionSpritePath = newExplosionSpritePath;
            explosionTint = newExplosionTint;
            effectSortingOrder = newEffectSortingOrder;

            transform.position = start + (Vector3.up * heightOffset);
            CreateProjectileVisual(projectileSpritePath, projectileTint, projectileScale, projectileSortingOrder);
        }

        private void CreateProjectileVisual(string spritePath, Color tint, Vector3 localScale, int sortingOrder)
        {
            if (string.IsNullOrWhiteSpace(spritePath))
            {
                return;
            }

            var sprite = Dagon.Core.RuntimeSpriteLibrary.LoadSprite(spritePath, 256f);
            if (sprite == null)
            {
                return;
            }

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(transform, false);
            visuals.transform.localScale = localScale == Vector3.zero ? new Vector3(0.35f, 0.35f, 1f) : localScale;

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingOrder = sortingOrder;

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);
        }

        private void Land()
        {
            switch (landingPayloadMode)
            {
                case LandingPayloadMode.HostilePool:
                    ShapedHazardZone.SpawnCircle(
                        targetPosition,
                        pulseRadius,
                        poolDuration,
                        poolTickDamage,
                        poolTickInterval,
                        worldCamera,
                        CombatTeam.Player,
                        owner,
                        new Color(0.34f, 0.86f, 0.58f, 0.36f),
                        new Color(0.76f, 0.98f, 0.82f, 0.72f),
                        poolSlowAmount,
                        poolSlowDuration,
                        tickImmediatelyOnEnter: true,
                        name: poolName);
                    break;
                default:
                    RotBeaconBombBeacon.Spawn(
                        targetPosition,
                        worldCamera,
                        owner,
                        pulseRadius,
                        pulseCount,
                        pulseInterval,
                        pulseDamage,
                        slowAmount,
                        slowDuration,
                        explosionRadius,
                        explosionDamage,
                        enemyMask,
                        beaconSpritePath,
                        beaconTint,
                        beaconScale,
                        beaconSortingOrder,
                        pulseSpritePath,
                        pulseTint,
                        explosionSpritePath,
                        explosionTint,
                        effectSortingOrder,
                        heightOffset);
                    break;
            }

            Destroy(gameObject);
        }

        private void InitializeHostilePool(
            Vector3 start,
            Vector3 target,
            float duration,
            float lobHeight,
            Camera camera,
            GameObject sourceOwner,
            string projectileSpritePath,
            Color projectileTint,
            Vector3 projectileScale,
            int projectileSortingOrder,
            float projectileHeightOffset,
            float radius,
            float zoneDuration,
            float tickDamage,
            float tickInterval,
            float appliedSlowAmount,
            float appliedSlowDuration,
            string hostilePoolName)
        {
            startPosition = start;
            targetPosition = target;
            travelDuration = Mathf.Max(0.05f, duration);
            arcHeight = Mathf.Max(0.05f, lobHeight);
            worldCamera = camera;
            owner = sourceOwner;
            heightOffset = projectileHeightOffset;
            landingPayloadMode = LandingPayloadMode.HostilePool;
            pulseRadius = Mathf.Max(0.1f, radius);
            poolDuration = Mathf.Max(0.1f, zoneDuration);
            poolTickDamage = Mathf.Max(0.01f, tickDamage);
            poolTickInterval = Mathf.Max(0.05f, tickInterval);
            poolSlowAmount = Mathf.Clamp01(appliedSlowAmount);
            poolSlowDuration = Mathf.Max(0f, appliedSlowDuration);
            poolName = string.IsNullOrWhiteSpace(hostilePoolName) ? "EnemyThrownPool" : hostilePoolName;

            transform.position = start + (Vector3.up * heightOffset);
            CreateProjectileVisual(projectileSpritePath, projectileTint, projectileScale, projectileSortingOrder);
        }
    }
}
