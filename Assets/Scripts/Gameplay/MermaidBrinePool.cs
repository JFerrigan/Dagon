using System.Collections.Generic;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MermaidBrinePool : MonoBehaviour
    {
        private float duration;
        private float tickDamage;
        private float tickInterval;
        private float slowAmount;
        private float slowDuration;
        private GameObject sourceOwner;
        private readonly HashSet<Hurtbox> occupants = new();
        private readonly Dictionary<Hurtbox, float> nextTickTimes = new();

        public static void Spawn(
            Vector3 position,
            float radius,
            float duration,
            float tickDamage,
            float tickInterval,
            float slowAmount,
            float slowDuration,
            Camera camera,
            GameObject owner,
            string name = "MermaidBrinePool")
        {
            var zone = new GameObject(name);
            zone.transform.position = position + Vector3.up * 0.08f;

            var component = zone.AddComponent<MermaidBrinePool>();
            component.Initialize(radius, duration, tickDamage, tickInterval, slowAmount, slowDuration, camera, owner);
        }

        private void Update()
        {
            duration -= Time.deltaTime;
            if (occupants.Count > 0)
            {
                TickOccupants();
            }

            if (duration <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void Initialize(
            float radius,
            float zoneDuration,
            float damage,
            float interval,
            float appliedSlowAmount,
            float appliedSlowDuration,
            Camera camera,
            GameObject owner)
        {
            duration = Mathf.Max(0.1f, zoneDuration);
            tickDamage = Mathf.Max(0.01f, damage);
            tickInterval = Mathf.Max(0.1f, interval);
            slowAmount = Mathf.Clamp01(appliedSlowAmount);
            slowDuration = Mathf.Max(0.1f, appliedSlowDuration);
            sourceOwner = owner;

            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = Mathf.Max(0.1f, radius);

            var body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            PlaceholderWeaponVisual.Spawn(
                "MermaidBrinePoolVisual",
                transform.position,
                new Vector3(radius * 2.15f, radius * 2.15f, 1f),
                camera,
                new Color(0.38f, 0.74f, 0.70f, 0.42f),
                zoneDuration,
                1f,
                0f,
                spritePath: "Sprites/Effects/brine_surge",
                pixelsPerUnit: 256f,
                sortingOrder: 4,
                groundPlane: true);
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
            if (!CombatResolver.TryResolveTarget(other, CombatTeam.Enemy, sourceOwner, out var hurtbox) || hurtbox == null || hurtbox.Team != CombatTeam.Player)
            {
                return;
            }

            occupants.Remove(hurtbox);
            nextTickTimes.Remove(hurtbox);
        }

        private void TryTrackOccupant(Collider other)
        {
            if (!CombatResolver.TryResolveTarget(other, CombatTeam.Enemy, sourceOwner, out var hurtbox) || hurtbox == null || hurtbox.Team != CombatTeam.Player)
            {
                return;
            }

            occupants.Add(hurtbox);
            if (!nextTickTimes.ContainsKey(hurtbox))
            {
                nextTickTimes[hurtbox] = Time.time;
            }

            hurtbox.GetComponentInParent<PlayerSlowReceiver>()?.ApplySlow(slowAmount, slowDuration);
        }

        private void TickOccupants()
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

                hurtbox.Damageable?.ApplyDamage(tickDamage, sourceOwner != null ? sourceOwner : gameObject);
                hurtbox.GetComponentInParent<PlayerSlowReceiver>()?.ApplySlow(slowAmount, slowDuration);
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
