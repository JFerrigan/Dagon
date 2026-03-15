using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class Hurtbox : MonoBehaviour
    {
        [SerializeField] private CombatTeam team = CombatTeam.Neutral;
        [SerializeField] private Health health;

        public CombatTeam Team => team;
        public IDamageable Damageable => health;
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
                $"configured object={name} team={team} health={(health != null ? health.name : "null")}",
                this);
        }
    }
}
