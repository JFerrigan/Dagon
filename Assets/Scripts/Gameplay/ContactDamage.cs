using UnityEngine;
using System.Collections.Generic;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ContactDamage : MonoBehaviour
    {
        [SerializeField] private float damage = 1f;
        [SerializeField] private float cooldown = 0.75f;
        [SerializeField] private CombatTeam attackingTeam = CombatTeam.Enemy;

        private Collider cachedCollider;
        private BodyBlocker cachedBodyBlocker;
        private float auraCadenceMultiplier = 1f;
        private readonly HashSet<Hurtbox> overlappingTargets = new();
        private readonly Dictionary<Hurtbox, float> nextDamageTimes = new();

        private void Awake()
        {
            cachedCollider = GetComponent<Collider>();
            cachedBodyBlocker = GetComponent<BodyBlocker>();
        }

        private void Update()
        {
            SyncBodyContactTargets();

            if (overlappingTargets.Count <= 0)
            {
                return;
            }

            var now = Time.time;
            var staleTargets = ListPool<Hurtbox>.Get();
            foreach (var target in overlappingTargets)
            {
                if (target == null)
                {
                    staleTargets.Add(target);
                    continue;
                }

                if (!nextDamageTimes.TryGetValue(target, out var nextDamageTime) || now < nextDamageTime)
                {
                    continue;
                }

                ApplyDamageTo(target);
            }

            for (var i = 0; i < staleTargets.Count; i++)
            {
                overlappingTargets.Remove(staleTargets[i]);
                nextDamageTimes.Remove(staleTargets[i]);
            }

            ListPool<Hurtbox>.Release(staleTargets);
        }

        public void Configure(float newDamage)
        {
            damage = Mathf.Max(0.01f, newDamage);
            attackingTeam = CombatResolver.GetTeam(gameObject);
            if (cachedCollider == null)
            {
                cachedCollider = GetComponent<Collider>();
            }

            if (cachedBodyBlocker == null)
            {
                cachedBodyBlocker = GetComponent<BodyBlocker>();
            }
        }

        public void SetAuraCadenceMultiplier(float multiplier)
        {
            auraCadenceMultiplier = Mathf.Max(1f, multiplier);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryTrackTarget(other, applyImmediately: true);
        }

        private void OnTriggerStay(Collider other)
        {
            TryTrackTarget(other, applyImmediately: false);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!TryResolveContactTarget(other, out var hurtbox))
            {
                return;
            }

            overlappingTargets.Remove(hurtbox);
            nextDamageTimes.Remove(hurtbox);
            CombatDebug.Log(
                "ContactDamage",
                $"source={name} removedTarget={CombatDebug.NameOf(hurtbox)}",
                this);
        }

        private void TryTrackTarget(Collider other, bool applyImmediately)
        {
            if (!TryResolveContactTarget(other, out var hurtbox))
            {
                return;
            }

            overlappingTargets.Add(hurtbox);
            if (!nextDamageTimes.ContainsKey(hurtbox))
            {
                nextDamageTimes[hurtbox] = Time.time;
            }

            if (applyImmediately && Time.time >= nextDamageTimes[hurtbox])
            {
                ApplyDamageTo(hurtbox);
            }
        }

        private void SyncBodyContactTargets()
        {
            if (cachedBodyBlocker == null)
            {
                return;
            }

            var staleTargets = ListPool<Hurtbox>.Get();
            foreach (var target in overlappingTargets)
            {
                if (target == null)
                {
                    staleTargets.Add(target);
                    continue;
                }

                if (!IsBodyContactTarget(target) && !IsTriggerContactTarget(target))
                {
                    staleTargets.Add(target);
                }
            }

            for (var i = 0; i < staleTargets.Count; i++)
            {
                overlappingTargets.Remove(staleTargets[i]);
                nextDamageTimes.Remove(staleTargets[i]);
            }

            staleTargets.Clear();

            var blockers = BodyBlocker.Active;
            for (var i = 0; i < blockers.Count; i++)
            {
                var other = blockers[i];
                if (!IsBodyContactCandidate(other))
                {
                    continue;
                }

                var hurtbox = other.GetComponent<Hurtbox>();
                if (hurtbox == null || hurtbox.Damageable == null)
                {
                    continue;
                }

                overlappingTargets.Add(hurtbox);
                if (!nextDamageTimes.ContainsKey(hurtbox))
                {
                    nextDamageTimes[hurtbox] = Time.time;
                }
            }

            ListPool<Hurtbox>.Release(staleTargets);
        }

        private bool TryResolveContactTarget(Collider other, out Hurtbox hurtbox)
        {
            hurtbox = null;
            if (cachedCollider == null || other == null || other == cachedCollider)
            {
                return false;
            }

            if (!CombatResolver.TryResolveTarget(other, attackingTeam, gameObject, out hurtbox))
            {
                return false;
            }

            return hurtbox != null;
        }

        private bool IsBodyContactCandidate(BodyBlocker other)
        {
            if (cachedBodyBlocker == null || other == null || other == cachedBodyBlocker)
            {
                return false;
            }

            if (!other.isActiveAndEnabled || other.Suppressed)
            {
                return false;
            }

            var otherHurtbox = other.GetComponent<Hurtbox>();
            if (otherHurtbox == null || otherHurtbox.Team == attackingTeam)
            {
                return false;
            }

            var combinedRadius = cachedBodyBlocker.BodyRadius + other.BodyRadius;
            var separation = cachedBodyBlocker.PlanarPosition - other.PlanarPosition;
            separation.y = 0f;
            return separation.sqrMagnitude <= combinedRadius * combinedRadius;
        }

        private bool IsBodyContactTarget(Hurtbox hurtbox)
        {
            if (hurtbox == null || cachedBodyBlocker == null)
            {
                return false;
            }

            var other = hurtbox.GetComponent<BodyBlocker>();
            return IsBodyContactCandidate(other);
        }

        private bool IsTriggerContactTarget(Hurtbox hurtbox)
        {
            if (hurtbox == null || cachedCollider == null)
            {
                return false;
            }

            var colliders = hurtbox.GetComponentsInChildren<Collider>();
            for (var i = 0; i < colliders.Length; i++)
            {
                var other = colliders[i];
                if (other == null || other == cachedCollider)
                {
                    continue;
                }

                if (cachedCollider.bounds.Intersects(other.bounds))
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyDamageTo(Hurtbox hurtbox)
        {
            if (hurtbox == null)
            {
                return;
            }

            hurtbox.Damageable.ApplyDamage(damage, gameObject);
            nextDamageTimes[hurtbox] = Time.time + (cooldown / auraCadenceMultiplier);
            CombatDebug.Log(
                "ContactDamage",
                $"source={name} hit={CombatDebug.NameOf(hurtbox)} damage={damage:0.##}",
                this);
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
