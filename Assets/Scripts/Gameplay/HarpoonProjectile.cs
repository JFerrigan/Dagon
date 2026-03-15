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
                Collider nearestValidHit = null;
                for (var i = 0; i < hits.Length; i++)
                {
                    var candidate = hits[i];
                    if (candidate.distance >= nearestDistance)
                    {
                        continue;
                    }

                    if (!CombatResolver.TryResolveTarget(candidate.collider, sourceTeam, owner, out _))
                    {
                        continue;
                    }

                    nearestDistance = candidate.distance;
                    nearestValidHit = candidate.collider;
                }

                if (nearestValidHit != null)
                {
                    CombatDebug.Log(
                        "HarpoonSweep",
                        $"projectile={name} owner={CombatDebug.NameOf(owner)} nearestValidHit={CombatDebug.NameOf(nearestValidHit)} distance={nearestDistance:0.###}",
                        this);
                    transform.position += direction * nearestDistance;
                    if (TryImpact(nearestValidHit))
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
                $"projectile={name} owner={CombatDebug.NameOf(owner)} hit={CombatDebug.NameOf(other)}",
                this);
            TryImpact(other);
        }

        private bool TryImpact(Collider other)
        {
            if (impacted || other == null)
            {
                CombatDebug.Log("HarpoonImpact", $"projectile={name} impactSkipped=true reason=already_impacted_or_null hit={CombatDebug.NameOf(other)}", this);
                return false;
            }

            if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == owner)
            {
                CombatDebug.Log("HarpoonImpact", $"projectile={name} impactSkipped=true reason=owner_rigidbody hit={CombatDebug.NameOf(other)}", this);
                return false;
            }

            if (other.gameObject == owner)
            {
                CombatDebug.Log("HarpoonImpact", $"projectile={name} impactSkipped=true reason=owner_object hit={CombatDebug.NameOf(other)}", this);
                return false;
            }

            if (!CombatResolver.TryApplyDamage(other, sourceTeam, owner, damage))
            {
                CombatDebug.Log(
                    "HarpoonImpact",
                    $"projectile={name} owner={CombatDebug.NameOf(owner)} hit={CombatDebug.NameOf(other)} applied=false",
                    this);
                return false;
            }

            if (applyKnockback)
            {
                CombatKnockback.TryApply(other, direction, knockbackStrength);
            }

            CombatDebug.Log(
                "HarpoonImpact",
                $"projectile={name} owner={CombatDebug.NameOf(owner)} hit={CombatDebug.NameOf(other)} applied=true",
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
