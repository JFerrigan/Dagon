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
        private readonly HashSet<Hurtbox> overlappingTargets = new();
        private readonly Dictionary<Hurtbox, float> nextDamageTimes = new();

        private void Awake()
        {
            cachedCollider = GetComponent<Collider>();
        }

        private void Update()
        {
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

        private void ApplyDamageTo(Hurtbox hurtbox)
        {
            if (hurtbox == null)
            {
                return;
            }

            hurtbox.Damageable.ApplyDamage(damage, gameObject);
            nextDamageTimes[hurtbox] = Time.time + cooldown;
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
