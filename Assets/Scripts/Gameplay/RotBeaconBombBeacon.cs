using System.Collections.Generic;
using Dagon.Core;
using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RotBeaconBombBeacon : MonoBehaviour
    {
        private static readonly Color PulseOverlayTint = new(0.26f, 0.92f, 0.38f, 0.34f);
        private static readonly Color DetonationOverlayTint = new(0.95f, 0.24f, 0.20f, 0.34f);
        private const float OverlayHeightOffset = 0.05f;
        private const float PulseOverlayThickness = 0.14f;
        private const float DetonationOverlayThickness = 0.18f;
        private readonly HashSet<GameObject> resolvedTargets = new();
        private readonly HashSet<GameObject> resolvedExplosionTargets = new();

        private Camera worldCamera;
        private GameObject owner;
        private float pulseRadius;
        private int remainingPulses;
        private float pulseInterval;
        private float pulseTimer;
        private float pulseDamage;
        private float slowAmount;
        private float slowDuration;
        private float explosionRadius;
        private float explosionDamage;
        private LayerMask enemyMask;
        private string pulseSpritePath;
        private Color pulseTint;
        private string explosionSpritePath;
        private Color explosionTint;
        private int effectSortingOrder;
        private bool detonated;

        internal static void Spawn(
            Vector3 position,
            Camera worldCamera,
            GameObject owner,
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
            int effectSortingOrder,
            float heightOffset)
        {
            var beacon = new GameObject("RotBeaconBombBeacon");
            beacon.transform.position = position + (Vector3.up * heightOffset);

            var component = beacon.AddComponent<RotBeaconBombBeacon>();
            component.Initialize(
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
                pulseSpritePath,
                pulseTint,
                explosionSpritePath,
                explosionTint,
                effectSortingOrder);
            component.CreateBeaconVisual(beaconSpritePath, beaconTint, beaconScale, beaconSortingOrder);
        }

        private void Update()
        {
            if (detonated)
            {
                return;
            }

            pulseTimer -= Time.deltaTime;
            if (pulseTimer > 0f)
            {
                return;
            }

            Pulse();
            remainingPulses -= 1;
            if (remainingPulses <= 0)
            {
                Detonate();
                return;
            }

            pulseTimer = pulseInterval;
        }

        private void Initialize(
            Camera newWorldCamera,
            GameObject newOwner,
            float newPulseRadius,
            int pulseCount,
            float newPulseInterval,
            float newPulseDamage,
            float newSlowAmount,
            float newSlowDuration,
            float newExplosionRadius,
            float newExplosionDamage,
            LayerMask newEnemyMask,
            string newPulseSpritePath,
            Color newPulseTint,
            string newExplosionSpritePath,
            Color newExplosionTint,
            int newEffectSortingOrder)
        {
            worldCamera = newWorldCamera;
            owner = newOwner;
            pulseRadius = Mathf.Max(0.1f, newPulseRadius);
            remainingPulses = Mathf.Max(1, pulseCount);
            pulseInterval = Mathf.Max(0.05f, newPulseInterval);
            pulseTimer = 0.01f;
            pulseDamage = Mathf.Max(0.1f, newPulseDamage);
            slowAmount = Mathf.Clamp01(newSlowAmount);
            slowDuration = Mathf.Max(0f, newSlowDuration);
            explosionRadius = Mathf.Max(0.1f, newExplosionRadius);
            explosionDamage = Mathf.Max(0.1f, newExplosionDamage);
            enemyMask = newEnemyMask;
            pulseSpritePath = newPulseSpritePath;
            pulseTint = newPulseTint;
            explosionSpritePath = newExplosionSpritePath;
            explosionTint = newExplosionTint;
            effectSortingOrder = newEffectSortingOrder;
        }

        private void CreateBeaconVisual(string spritePath, Color tint, Vector3 localScale, int sortingOrder)
        {
            if (string.IsNullOrWhiteSpace(spritePath))
            {
                return;
            }

            var sprite = RuntimeSpriteLibrary.LoadSprite(spritePath, 256f);
            if (sprite == null)
            {
                return;
            }

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(transform, false);
            visuals.transform.localScale = localScale == Vector3.zero ? new Vector3(0.5f, 0.5f, 1f) : localScale;

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingOrder = sortingOrder;

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);
        }

        private void Pulse()
        {
            resolvedTargets.Clear();
            var colliders = Physics.OverlapSphere(transform.position, pulseRadius, enemyMask, QueryTriggerInteraction.Collide);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (!CombatResolver.TryResolveUniqueHit(colliders[i], CombatTeam.Player, owner, resolvedTargets, out var resolvedHit))
                {
                    continue;
                }

                CombatResolver.TryApplyDamage(resolvedHit, owner, pulseDamage, CombatTeam.Player);
                ApplySlow(resolvedHit);
            }

            RotLanternRadiusVisual.Spawn(
                transform.position,
                pulseRadius,
                OverlayHeightOffset,
                PulseOverlayThickness,
                PulseOverlayTint,
                pulseInterval * 0.9f,
                1.03f,
                effectSortingOrder);

            PlaceholderWeaponVisual.Spawn(
                "RotBeaconPulse",
                transform.position,
                new Vector3(pulseRadius * 2.2f, pulseRadius * 2.2f, 1f),
                worldCamera,
                pulseTint,
                pulseInterval * 0.9f,
                1.04f,
                0f,
                spritePath: string.IsNullOrWhiteSpace(pulseSpritePath) ? "Sprites/Effects/brine_surge" : pulseSpritePath,
                pixelsPerUnit: 256f,
                sortingOrder: effectSortingOrder,
                groundPlane: true);
        }

        private void Detonate()
        {
            if (detonated)
            {
                return;
            }

            detonated = true;
            resolvedExplosionTargets.Clear();
            var colliders = Physics.OverlapSphere(transform.position, explosionRadius, enemyMask, QueryTriggerInteraction.Collide);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (!CombatResolver.TryResolveUniqueHit(colliders[i], CombatTeam.Player, owner, resolvedExplosionTargets, out var resolvedHit))
                {
                    continue;
                }

                CombatResolver.TryApplyDamage(resolvedHit, owner, explosionDamage, CombatTeam.Player);
            }

            RotLanternRadiusVisual.Spawn(
                transform.position,
                explosionRadius,
                OverlayHeightOffset,
                DetonationOverlayThickness,
                DetonationOverlayTint,
                0.28f,
                1.08f,
                effectSortingOrder + 1);

            PlaceholderWeaponVisual.Spawn(
                "RotBeaconExplosion",
                transform.position + Vector3.up * 0.05f,
                new Vector3(explosionRadius * 2.1f, explosionRadius * 2.1f, 1f),
                worldCamera,
                explosionTint,
                0.28f,
                1.14f,
                0f,
                spritePath: string.IsNullOrWhiteSpace(explosionSpritePath) ? "Sprites/Effects/brine_surge" : explosionSpritePath,
                pixelsPerUnit: 256f,
                sortingOrder: effectSortingOrder + 2,
                groundPlane: true);

            Destroy(gameObject, 0.05f);
        }

        private void ApplySlow(CombatHitResult resolvedHit)
        {
            var slowReceiver = resolvedHit.TargetRoot != null ? resolvedHit.TargetRoot.GetComponent<EnemySlowReceiver>() : null;
            if (slowReceiver == null && resolvedHit.Hurtbox != null && resolvedHit.Hurtbox.Health != null)
            {
                slowReceiver = resolvedHit.Hurtbox.Health.GetComponent<EnemySlowReceiver>() ??
                               resolvedHit.Hurtbox.Health.gameObject.AddComponent<EnemySlowReceiver>();
            }

            slowReceiver?.ApplySlow(slowAmount, slowDuration);
        }
    }
}
