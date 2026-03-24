using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class Hurtbox : MonoBehaviour
    {
        [SerializeField] private CombatTeam team = CombatTeam.Neutral;
        [SerializeField] private Health health;
        [SerializeField] private MonoBehaviour damageableOverride;

        public CombatTeam Team => team;
        public IDamageable Damageable => damageableOverride as IDamageable ?? health;
        public Health Health => health;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }
        }

        public void Configure(CombatTeam newTeam, Health targetHealth = null)
        {
            team = newTeam;
            if (targetHealth != null)
            {
                health = targetHealth;
            }
            else if (health == null)
            {
                health = GetComponent<Health>();
            }

            CombatDebug.Log(
                "Hurtbox",
                $"configured object={name} team={team} health={(health != null ? health.name : "null")} override={(damageableOverride != null ? damageableOverride.name : "null")}",
                this);
        }

        public void SetDamageableOverride(MonoBehaviour overrideBehaviour)
        {
            damageableOverride = overrideBehaviour;
        }
    }
}
