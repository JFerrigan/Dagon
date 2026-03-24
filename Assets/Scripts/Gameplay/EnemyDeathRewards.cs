using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EnemyDeathRewards : MonoBehaviour
    {
        private const float DualDropSeparation = 0.55f;

        [SerializeField] private int experienceReward = 1;
        [SerializeField] private float corruptionReward = 1.5f;
        [SerializeField] [Range(0f, 1f)] private float healthPickupDropChance;
        [SerializeField] private float healthPickupHealAmount = 2f;
        [SerializeField] private Health health;
        [SerializeField] private bool dropAtColliderEdge;
        [SerializeField] private float dropEdgePadding = 0.8f;

        private Camera worldCamera;
        private Collider cachedCollider;
        private Transform player;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }
            worldCamera = Camera.main;
            cachedCollider = GetComponent<Collider>();
            var playerObject = FindObjectOfType<PlayerMover>();
            player = playerObject != null ? playerObject.transform : null;
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
            }
        }

        public void Configure(int newExperienceReward, float newCorruptionReward, float newHealthPickupDropChance = 0f, float newHealthPickupHealAmount = 2f)
        {
            experienceReward = Mathf.Max(0, newExperienceReward);
            corruptionReward = Mathf.Max(0f, newCorruptionReward);
            healthPickupDropChance = Mathf.Clamp01(newHealthPickupDropChance);
            healthPickupHealAmount = Mathf.Max(0.1f, newHealthPickupHealAmount);
        }

        public void ConfigureDropAtColliderEdge(bool enabled, float padding = 0.8f)
        {
            dropAtColliderEdge = enabled;
            dropEdgePadding = Mathf.Max(0f, padding);
        }

        private void HandleDeath(Health _, GameObject source)
        {
            var dropPosition = ResolveDropPosition(source);
            var dropLateral = ResolveDropLateral(source);
            var experienceDropPosition = dropPosition;
            var corruptionDropPosition = dropPosition;
            if (experienceReward > 0 && corruptionReward > 0f)
            {
                experienceDropPosition += dropLateral * -DualDropSeparation;
                corruptionDropPosition += dropLateral * DualDropSeparation;
            }

            if (experienceReward > 0)
            {
                ExperiencePickup.Create(experienceDropPosition, experienceReward, worldCamera);
            }

            if (corruptionReward > 0f)
            {
                CorruptionPickup.Create(corruptionDropPosition, corruptionReward, worldCamera);
            }

            if (healthPickupDropChance > 0f && Random.value <= healthPickupDropChance)
            {
                HealthPickup.Create(dropPosition, healthPickupHealAmount, worldCamera);
            }
        }

        private Vector3 ResolveDropPosition(GameObject source)
        {
            if (!dropAtColliderEdge)
            {
                return transform.position;
            }

            var anchor = transform.position;
            Vector3 targetPosition;
            if (source != null)
            {
                targetPosition = source.transform.position;
            }
            else if (player != null)
            {
                targetPosition = player.position;
            }
            else
            {
                targetPosition = anchor + Vector3.forward;
            }

            var direction = targetPosition - anchor;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector3.forward;
            }

            var edgeDistance = ResolveDropEdgeDistance();
            return anchor + (direction.normalized * edgeDistance);
        }

        private Vector3 ResolveDropLateral(GameObject source)
        {
            var anchor = transform.position;
            Vector3 targetPosition;
            if (source != null)
            {
                targetPosition = source.transform.position;
            }
            else if (player != null)
            {
                targetPosition = player.position;
            }
            else
            {
                targetPosition = anchor + Vector3.forward;
            }

            var forward = targetPosition - anchor;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            return new Vector3(-forward.z, 0f, forward.x);
        }

        private float ResolveDropEdgeDistance()
        {
            return cachedCollider switch
            {
                CapsuleCollider capsule => Mathf.Max(capsule.radius + dropEdgePadding, 0.2f),
                SphereCollider sphere => Mathf.Max(sphere.radius + dropEdgePadding, 0.2f),
                _ => Mathf.Max(dropEdgePadding, 0.2f)
            };
        }
    }
}
