using System.Collections.Generic;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SwordfishDashTrail : MonoBehaviour
    {
        private static readonly Color TrailFillTint = new(0.18f, 0.78f, 0.86f, 0.34f);
        private static readonly Color TrailOverlayTint = new(0.78f, 0.98f, 0.96f, 0.2f);

        private float duration;
        private float tickDamage;
        private float tickInterval;
        private GameObject sourceOwner;
        private readonly Dictionary<Hurtbox, float> nextTickTimes = new();
        private readonly HashSet<Hurtbox> occupants = new();

        public static void Spawn(
            Vector3 start,
            Vector3 end,
            float width,
            float duration,
            float tickDamage,
            float tickInterval,
            Camera camera,
            GameObject owner)
        {
            var trail = new GameObject("SwordfishDashTrail");
            var planarDelta = end - start;
            planarDelta.y = 0f;
            var midpoint = start + (planarDelta * 0.5f);
            trail.transform.position = midpoint + Vector3.up * 0.05f;
            trail.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(planarDelta.x, planarDelta.z) * Mathf.Rad2Deg, 0f);

            var component = trail.AddComponent<SwordfishDashTrail>();
            component.Initialize(planarDelta.magnitude, width, duration, tickDamage, tickInterval, camera, owner);
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
            float length,
            float width,
            float trailDuration,
            float damage,
            float interval,
            Camera camera,
            GameObject owner)
        {
            duration = Mathf.Max(0.1f, trailDuration);
            tickDamage = Mathf.Max(0.01f, damage);
            tickInterval = Mathf.Max(0.05f, interval);
            sourceOwner = owner;

            var collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(Mathf.Max(0.15f, width), 0.75f, Mathf.Max(0.5f, length));
            collider.center = new Vector3(0f, 0.2f, 0f);

            var body = gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;

            var halfLength = Mathf.Max(0.25f, length * 0.5f);
            var direction = transform.forward;
            var start = transform.position - (direction * halfLength);
            var end = transform.position + (direction * halfLength);
            LineAttackOverlayVisual.Spawn(
                "SwordfishDashTrail",
                start,
                end,
                width,
                trailDuration,
                camera,
                TrailFillTint,
                TrailOverlayTint);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryTrack(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryTrack(other);
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

        private void TryTrack(Collider other)
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

            return hurtbox != null && hurtbox.Team == CombatTeam.Player;
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
