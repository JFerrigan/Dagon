using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class HarpoonProjectile : MonoBehaviour
    {
        [SerializeField] private float lifetime = 2f;

        private GameObject owner;
        private Vector3 direction = Vector3.forward;
        private float speed = 10f;
        private float damage = 1f;
        private bool impacted;
        private float collisionRadius;
        private CombatTeam sourceTeam;
        private bool applyKnockback;
        private float knockbackStrength;

        public void Initialize(
            GameObject projectileOwner,
            Vector3 moveDirection,
            float moveSpeed,
            float projectileDamage,
            bool enableKnockback = false,
            float hitKnockbackStrength = 0f)
        {
            owner = projectileOwner;
            direction = moveDirection.normalized;
            speed = moveSpeed;
            damage = projectileDamage;
            impacted = false;
            collisionRadius = ResolveCollisionRadius();
            sourceTeam = CombatResolver.GetTeam(projectileOwner);
            applyKnockback = enableKnockback;
            knockbackStrength = Mathf.Max(0f, hitKnockbackStrength);
            CombatDebug.Log(
                "HarpoonInit",
                $"projectile={name} owner={CombatDebug.NameOf(projectileOwner)} sourceTeam={sourceTeam} speed={speed:0.##} damage={damage:0.##} knockback={applyKnockback}:{knockbackStrength:0.##}",
                this);
        }

        private void Update()
        {
            if (impacted)
            {
                return;
            }

            var step = direction * (speed * Time.deltaTime);
            if (step.sqrMagnitude > 0f)
            {
                var distance = step.magnitude;
                var hits = Physics.SphereCastAll(transform.position, collisionRadius, direction, distance, ~0, QueryTriggerInteraction.Collide);
                var nearestDistance = float.MaxValue;
                CombatHitResult nearestHit = default;
                for (var i = 0; i < hits.Length; i++)
                {
                    var candidate = hits[i];
                    if (candidate.distance >= nearestDistance)
                    {
                        continue;
                    }

                    var resolvedHit = CombatResolver.ResolveHit(candidate.collider, sourceTeam, owner);
                    if (!resolvedHit.BlocksImpact)
                    {
                        continue;
                    }

                    nearestDistance = candidate.distance;
                    nearestHit = resolvedHit;
                }

                if (nearestHit.Collider != null)
                {
                    CombatDebug.Log(
                        "HarpoonSweep",
                        $"projectile={name} owner={CombatDebug.NameOf(owner)} nearestValidHit={CombatDebug.NameOf(nearestHit.Collider)} distance={nearestDistance:0.###} result={nearestHit.Type}",
                        this);
                    transform.position += direction * nearestDistance;
                    if (TryImpact(nearestHit))
                    {
                        return;
                    }
                }
            }

            transform.position += step;
            lifetime -= Time.deltaTime;

            if (lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            CombatDebug.Log(
                "HarpoonTrigger",
                $"projectile={name} owner={CombatDebug.NameOf(owner)} hit={CombatDebug.NameOf(other)} ignored=true reason=sweep_authority",
                this);
        }

        private bool TryImpact(CombatHitResult hit)
        {
            if (impacted || hit.Collider == null)
            {
                CombatDebug.Log("HarpoonImpact", $"projectile={name} impactSkipped=true reason=already_impacted_or_null hit={CombatDebug.NameOf(hit.Collider)}", this);
                return false;
            }

            if (!hit.BlocksImpact)
            {
                CombatDebug.Log("HarpoonImpact", $"projectile={name} impactSkipped=true reason=non_blocking hit={CombatDebug.NameOf(hit.Collider)}", this);
                return false;
            }

            if (!hit.CanApplyDamage)
            {
                CombatDebug.Log(
                    "HarpoonImpact",
                    $"projectile={name} owner={CombatDebug.NameOf(owner)} hit={CombatDebug.NameOf(hit.Collider)} applied=false result={hit.Type} reason={hit.Reason}",
                    this);
                impacted = true;
                Destroy(gameObject);
                return true;
            }

            hit.Damageable.ApplyDamage(damage, owner);
            if (applyKnockback)
            {
                CombatKnockback.TryApply(hit.Collider, direction, knockbackStrength);
            }

            CombatDebug.Log(
                "HarpoonImpact",
                $"projectile={name} owner={CombatDebug.NameOf(owner)} hit={CombatDebug.NameOf(hit.Collider)} applied=true",
                this);
            impacted = true;
            Destroy(gameObject);
            return true;
        }

        private float ResolveCollisionRadius()
        {
            var sphere = GetComponent<SphereCollider>();
            if (sphere != null)
            {
                var scale = Mathf.Max(1f, Mathf.Max(transform.lossyScale.x, transform.lossyScale.z));
                return Mathf.Max(0.05f, sphere.radius * scale);
            }

            return 0.1f;
        }
    }
}
