using System.Collections.Generic;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class FloodlineWave : MonoBehaviour
    {
        private readonly HashSet<GameObject> resolvedTargets = new();

        private Vector3 direction;
        private float travelSpeed;
        private float remainingDistance;
        private float length;
        private float halfWidth;
        private float hitboxHeight;
        private float damage;
        private float knockbackForce;
        private LayerMask enemyMask;
        private GameObject owner;

        internal static void Spawn(
            Vector3 origin,
            Vector3 forward,
            float moveSpeed,
            float maxTravelDistance,
            float waveLength,
            float waveHalfWidth,
            float hitboxHeight,
            float forwardOffset,
            float damage,
            float knockbackForce,
            LayerMask enemyMask,
            GameObject owner,
            FloodlineWeapon.VisualResolved visualPreset)
        {
            var wave = new GameObject("FloodlineWave");
            var normalizedForward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            var yaw = Mathf.Atan2(-normalizedForward.z, normalizedForward.x) * Mathf.Rad2Deg;
            wave.transform.position = origin + (normalizedForward * (Mathf.Max(0f, forwardOffset) + (Mathf.Max(0.25f, waveLength) * 0.5f)));
            wave.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var component = wave.AddComponent<FloodlineWave>();
            component.Initialize(
                normalizedForward,
                moveSpeed,
                maxTravelDistance,
                waveLength,
                waveHalfWidth,
                hitboxHeight,
                damage,
                knockbackForce,
                enemyMask,
                owner);

            FloodlineWaveVisual.Attach(wave.transform, waveLength, waveHalfWidth, maxTravelDistance / Mathf.Max(0.1f, moveSpeed), visualPreset);
        }

        private void Update()
        {
            var step = travelSpeed * Time.deltaTime;
            if (step <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            var moveDistance = Mathf.Min(step, remainingDistance);
            transform.position += direction * moveDistance;
            remainingDistance -= moveDistance;

            ResolveHits();

            if (remainingDistance <= 0.001f)
            {
                Destroy(gameObject);
            }
        }

        private void Initialize(
            Vector3 travelDirection,
            float moveSpeed,
            float maxTravelDistance,
            float waveLength,
            float waveHalfWidth,
            float waveHitboxHeight,
            float waveDamage,
            float waveKnockbackForce,
            LayerMask collisionMask,
            GameObject sourceOwner)
        {
            direction = travelDirection;
            travelSpeed = Mathf.Max(0.1f, moveSpeed);
            remainingDistance = Mathf.Max(0.1f, maxTravelDistance);
            length = Mathf.Max(0.25f, waveLength);
            halfWidth = Mathf.Max(0.2f, waveHalfWidth);
            hitboxHeight = Mathf.Max(0.5f, waveHitboxHeight);
            damage = Mathf.Max(0.1f, waveDamage);
            knockbackForce = Mathf.Max(0f, waveKnockbackForce);
            enemyMask = collisionMask;
            owner = sourceOwner;

            ResolveHits();
        }

        private void ResolveHits()
        {
            var halfExtents = new Vector3(length * 0.5f, hitboxHeight * 0.5f, halfWidth);
            var colliders = Physics.OverlapBox(transform.position, halfExtents, transform.rotation, enemyMask, QueryTriggerInteraction.Collide);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (!CombatResolver.TryResolveUniqueHit(colliders[i], CombatTeam.Player, owner, resolvedTargets, out var resolvedHit))
                {
                    continue;
                }

                CombatResolver.TryApplyDamage(resolvedHit, owner, damage, CombatTeam.Player);
                CombatKnockback.TryApply(resolvedHit.Collider, direction, knockbackForce);
            }
        }
    }
}
