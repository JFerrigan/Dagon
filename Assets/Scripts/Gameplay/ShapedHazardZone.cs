using System.Collections.Generic;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ShapedHazardZone : MonoBehaviour
    {
        public enum HazardShape
        {
            Circle,
            Box
        }

        private float duration;
        private float tickDamage;
        private float tickInterval;
        private float slowAmount;
        private float slowDuration;
        private bool tickImmediatelyOnEnter;
        private CombatTeam targetTeam = CombatTeam.Player;
        private GameObject sourceOwner;
        private readonly HashSet<Hurtbox> occupants = new();
        private readonly Dictionary<Hurtbox, float> nextTickTimes = new();

        public static void SpawnCircle(
            Vector3 position,
            float radius,
            float duration,
            float tickDamage,
            float tickInterval,
            Camera camera,
            CombatTeam targetTeam,
            GameObject owner,
            Color fillTint,
            Color outlineTint,
            float slowAmount = 0f,
            float slowDuration = 0.1f,
            bool tickImmediatelyOnEnter = false,
            string name = "ShapedHazardZone")
        {
            var zone = new GameObject(name);
            zone.transform.position = position + Vector3.up * 0.08f;

            var component = zone.AddComponent<ShapedHazardZone>();
            component.InitializeCircle(
                radius,
                duration,
                tickDamage,
                tickInterval,
                camera,
                targetTeam,
                owner,
                fillTint,
                outlineTint,
                slowAmount,
                slowDuration,
                tickImmediatelyOnEnter);
        }

        public static void SpawnBox(
            Vector3 start,
            Vector3 end,
            float width,
            float duration,
            float tickDamage,
            float tickInterval,
            Camera camera,
            CombatTeam targetTeam,
            GameObject owner,
            Color fillTint,
            Color innerTint,
            float slowAmount = 0f,
            float slowDuration = 0.1f,
            bool tickImmediatelyOnEnter = false,
            string name = "ShapedHazardZone")
        {
            var planarDelta = end - start;
            planarDelta.y = 0f;
            var length = planarDelta.magnitude;
            if (length <= 0.05f)
            {
                return;
            }

            var zone = new GameObject(name);
            zone.transform.position = start + (planarDelta * 0.5f) + Vector3.up * 0.05f;
            zone.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(planarDelta.x, planarDelta.z) * Mathf.Rad2Deg, 0f);

            var component = zone.AddComponent<ShapedHazardZone>();
            component.InitializeBox(
                width,
                length,
                duration,
                tickDamage,
                tickInterval,
                camera,
                targetTeam,
                owner,
                fillTint,
                innerTint,
                slowAmount,
                slowDuration,
                tickImmediatelyOnEnter);
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

        private void InitializeCircle(
            float radius,
            float zoneDuration,
            float damage,
            float interval,
            Camera camera,
            CombatTeam newTargetTeam,
            GameObject owner,
            Color fillTint,
            Color outlineTint,
            float appliedSlowAmount,
            float appliedSlowDuration,
            bool shouldTickImmediatelyOnEnter)
        {
            duration = Mathf.Max(0.1f, zoneDuration);
            tickDamage = Mathf.Max(0.01f, damage);
            tickInterval = Mathf.Max(0.05f, interval);
            slowAmount = Mathf.Clamp01(appliedSlowAmount);
            slowDuration = Mathf.Max(0.1f, appliedSlowDuration);
            tickImmediatelyOnEnter = shouldTickImmediatelyOnEnter;
            targetTeam = newTargetTeam;
            sourceOwner = owner;

            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = Mathf.Max(0.1f, radius);

            var body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            PlaceholderWeaponVisual.Spawn(
                "ShapedHazardCircleFill",
                transform.position,
                new Vector3(radius * 2.15f, radius * 2.15f, 1f),
                camera,
                fillTint,
                zoneDuration,
                1f,
                0f,
                spritePath: "Sprites/Effects/brine_surge",
                pixelsPerUnit: 256f,
                sortingOrder: 4,
                groundPlane: true);

            RotLanternRadiusVisual.Spawn(
                transform.position,
                radius,
                0.04f,
                0.16f,
                outlineTint,
                zoneDuration,
                1f,
                5);
        }

        private void InitializeBox(
            float width,
            float length,
            float zoneDuration,
            float damage,
            float interval,
            Camera camera,
            CombatTeam newTargetTeam,
            GameObject owner,
            Color fillTint,
            Color innerTint,
            float appliedSlowAmount,
            float appliedSlowDuration,
            bool shouldTickImmediatelyOnEnter)
        {
            duration = Mathf.Max(0.1f, zoneDuration);
            tickDamage = Mathf.Max(0.01f, damage);
            tickInterval = Mathf.Max(0.05f, interval);
            slowAmount = Mathf.Clamp01(appliedSlowAmount);
            slowDuration = Mathf.Max(0.1f, appliedSlowDuration);
            tickImmediatelyOnEnter = shouldTickImmediatelyOnEnter;
            targetTeam = newTargetTeam;
            sourceOwner = owner;

            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(Mathf.Max(0.15f, width), 0.75f, Mathf.Max(0.5f, length));
            box.center = new Vector3(0f, 0.2f, 0f);

            var body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var halfLength = Mathf.Max(0.25f, length * 0.5f);
            var direction = transform.forward;
            var start = transform.position - (direction * halfLength);
            var end = transform.position + (direction * halfLength);
            LineAttackOverlayVisual.Spawn(
                "ShapedHazardBox",
                start,
                end,
                width,
                zoneDuration,
                camera,
                fillTint,
                innerTint);
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
                nextTickTimes[hurtbox] = tickImmediatelyOnEnter ? Time.time : Time.time + tickInterval;
            }

            ApplySlow(hurtbox);
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
                ApplySlow(hurtbox);
                nextTickTimes[hurtbox] = now + tickInterval;
            }

            for (var i = 0; i < staleTargets.Count; i++)
            {
                occupants.Remove(staleTargets[i]);
                nextTickTimes.Remove(staleTargets[i]);
            }

            ListPool<Hurtbox>.Release(staleTargets);
        }

        private void ApplySlow(Hurtbox hurtbox)
        {
            if (hurtbox == null || slowAmount <= 0f)
            {
                return;
            }

            hurtbox.GetComponentInParent<PlayerSlowReceiver>()?.ApplySlow(slowAmount, slowDuration);
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
