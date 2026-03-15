using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ContactDamage : MonoBehaviour
    {
        [SerializeField] private float damage = 1f;
        [SerializeField] private float cooldown = 0.75f;
        [SerializeField] private CombatTeam attackingTeam = CombatTeam.Enemy;

        private float cooldownTimer;
        private Collider cachedCollider;

        private void Awake()
        {
            cachedCollider = GetComponent<Collider>();
        }

        private void Update()
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer > 0f)
            {
                return;
            }

            if (!TryApplyContactDamage())
            {
                return;
            }

            cooldownTimer = cooldown;
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

        private bool TryApplyContactDamage()
        {
            if (cachedCollider == null)
            {
                CombatDebug.Log("ContactDamage", $"source={name} applied=false reason=no_cached_collider", this);
                return false;
            }

            var bounds = cachedCollider.bounds;
            var colliders = Physics.OverlapBox(bounds.center, bounds.extents, transform.rotation, ~0, QueryTriggerInteraction.Collide);
            CombatDebug.Log(
                "ContactDamage",
                $"source={name} sourceTeam={attackingTeam} candidates={colliders.Length} damage={damage:0.##}",
                this);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == cachedCollider)
                {
                    continue;
                }

                if (CombatResolver.TryApplyDamage(colliders[i], attackingTeam, gameObject, damage))
                {
                    CombatDebug.Log(
                        "ContactDamage",
                        $"source={name} hit={CombatDebug.NameOf(colliders[i])} applied=true",
                        this);
                    return true;
                }
            }

            CombatDebug.Log("ContactDamage", $"source={name} applied=false reason=no_valid_targets", this);
            return false;
        }
    }
}
