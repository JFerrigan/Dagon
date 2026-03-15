using UnityEngine;
using System.Collections.Generic;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EnemyHazardZone : MonoBehaviour
    {
        private float radius;
        private float duration;
        private float tickDamage;
        private float tickInterval;
        private Camera worldCamera;
        private CombatTeam targetTeam = CombatTeam.Player;
        private GameObject sourceOwner;
        private readonly Dictionary<Hurtbox, float> nextTickTimes = new();
        private readonly HashSet<Hurtbox> occupants = new();

        public static void Spawn(
            Vector3 position,
            float radius,
            float duration,
            float tickDamage,
            float tickInterval,
            Camera camera,
            Color tint,
            string name = "EnemyHazardZone")
        {
            var zone = new GameObject(name);
            zone.transform.position = position + Vector3.up * 0.08f;

            var component = zone.AddComponent<EnemyHazardZone>();
            component.Initialize(radius, duration, tickDamage, tickInterval, camera, tint, CombatTeam.Player, null);
        }

        public static void SpawnForTeam(
            Vector3 position,
            float radius,
            float duration,
            float tickDamage,
            float tickInterval,
            Camera camera,
            Color tint,
            CombatTeam newTargetTeam,
            GameObject owner,
            string name = "EnemyHazardZone")
        {
            var zone = new GameObject(name);
            zone.transform.position = position + Vector3.up * 0.08f;

            var component = zone.AddComponent<EnemyHazardZone>();
            component.Initialize(radius, duration, tickDamage, tickInterval, camera, tint, newTargetTeam, owner);
        }

        private void Update()
        {
            duration -= Time.deltaTime;
            if (occupants.Count > 0)
            {
                TickDamage();
            }

            if (duration <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void Initialize(
            float zoneRadius,
            float zoneDuration,
            float damage,
            float interval,
            Camera camera,
            Color tint,
            CombatTeam newTargetTeam,
            GameObject owner)
        {
            radius = Mathf.Max(0.1f, zoneRadius);
            duration = Mathf.Max(0.1f, zoneDuration);
            tickDamage = Mathf.Max(0.01f, damage);
            tickInterval = Mathf.Max(0.1f, interval);
            worldCamera = camera;
            targetTeam = newTargetTeam;
            sourceOwner = owner;

            var sphere = GetOrAddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = radius;

            var body = GetOrAddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;

            PlaceholderWeaponVisual.Spawn(
                "EnemyHazardVisual",
                transform.position,
                new Vector3(radius * 2f, radius * 2f, 1f),
                worldCamera,
                tint,
                zoneDuration,
                1f);
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            if (gameObject.TryGetComponent<T>(out var component))
            {
                return component;
            }

            return gameObject.AddComponent<T>();
        }

        private void OnTriggerEnter(Collider other)
        {
            TryTrackOccupant(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryTrackOccupant(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!TryResolveOccupant(other, out var hurtbox))
            {
                return;
            }

            occupants.Remove(hurtbox);
            nextTickTimes.Remove(hurtbox);
        }

        private void TryTrackOccupant(Collider other)
        {
            if (!TryResolveOccupant(other, out var hurtbox))
            {
                return;
            }

            occupants.Add(hurtbox);
            if (!nextTickTimes.ContainsKey(hurtbox))
            {
                nextTickTimes[hurtbox] = Time.time + tickInterval;
            }
        }

        private bool TryResolveOccupant(Collider other, out Hurtbox hurtbox)
        {
            hurtbox = null;
            if (!CombatResolver.TryResolveTarget(other, CombatTeam.Neutral, sourceOwner, out hurtbox))
            {
                return false;
            }

            return hurtbox != null && hurtbox.Team == targetTeam;
        }

        private void TickDamage()
        {
            var now = Time.time;
            var staleTargets = ListPool<Hurtbox>.Get();
            foreach (var hurtbox in occupants)
            {
                if (hurtbox == null)
                {
                    staleTargets.Add(hurtbox);
                    continue;
                }

                if (!nextTickTimes.TryGetValue(hurtbox, out var nextTickTime) || now < nextTickTime)
                {
                    continue;
                }

                hurtbox.Damageable.ApplyDamage(tickDamage, sourceOwner != null ? sourceOwner : gameObject);
                nextTickTimes[hurtbox] = now + tickInterval;
            }

            for (var i = 0; i < staleTargets.Count; i++)
            {
                occupants.Remove(staleTargets[i]);
                nextTickTimes.Remove(staleTargets[i]);
            }

            ListPool<Hurtbox>.Release(staleTargets);
        }

        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new();

            public static List<T> Get()
            {
                return Pool.Count > 0 ? Pool.Pop() : new List<T>();
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
