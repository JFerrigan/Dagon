using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DrownedAcolyteProjectile : MonoBehaviour
    {
        [SerializeField] private float lifetime = 2.2f;

        private GameObject owner;
        private Vector3 direction = Vector3.forward;
        private float speed = 6.5f;
        private float impactDamage = 1f;
        private float hazardRadius = 1.2f;
        private float hazardDuration = 2.25f;
        private float hazardTickDamage = 0.5f;
        private float hazardTickInterval = 0.5f;
        private Camera worldCamera;
        private CombatTeam sourceTeam;
        private float collisionRadius;
        private bool impacted;

        public void Initialize(
            GameObject projectileOwner,
            Vector3 moveDirection,
            float moveSpeed,
            float directDamage,
            float zoneRadius,
            float zoneDuration,
            float zoneTickDamage,
            float zoneTickInterval,
            Camera cameraReference)
        {
            owner = projectileOwner;
            direction = moveDirection.normalized;
            speed = moveSpeed;
            impactDamage = directDamage;
            hazardRadius = zoneRadius;
            hazardDuration = zoneDuration;
            hazardTickDamage = zoneTickDamage;
            hazardTickInterval = zoneTickInterval;
            worldCamera = cameraReference;
            sourceTeam = CombatResolver.GetTeam(projectileOwner);
            collisionRadius = ResolveCollisionRadius();
            impacted = false;
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
                    transform.position += direction * nearestDistance;
                    Impact(nearestHit);
                    return;
                }
            }

            transform.position += step;
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Explode();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            CombatDebug.Log(
                "AcolyteTrigger",
                $"projectile={name} owner={CombatDebug.NameOf(owner)} hit={CombatDebug.NameOf(other)} ignored=true reason=sweep_authority",
                this);
        }

        private void Impact(CombatHitResult hit)
        {
            if (impacted || hit.Collider == null || !hit.BlocksImpact)
            {
                return;
            }

            if (hit.CanApplyDamage)
            {
                hit.Damageable.ApplyDamage(impactDamage, owner);
            }

            CombatDebug.Log(
                "AcolyteImpact",
                $"projectile={name} owner={CombatDebug.NameOf(owner)} hit={CombatDebug.NameOf(hit.Collider)} result={hit.Type} reason={hit.Reason}",
                this);
            Explode();
        }

        private void Explode()
        {
            if (impacted)
            {
                return;
            }

            impacted = true;
            EnemyHazardZone.SpawnForTeam(
                transform.position,
                hazardRadius,
                hazardDuration,
                hazardTickDamage,
                hazardTickInterval,
                worldCamera,
                new Color(0.38f, 0.82f, 0.52f, 0.48f),
                CombatTeam.Player,
                owner,
                "AcolyteHazardZone");
            Destroy(gameObject);
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
