using Dagon.Core;
using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EnemyDefinitionBinder : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private Health health;
        [SerializeField] private SimpleEnemyChaser enemyChaser;
        [SerializeField] private ContactDamage contactDamage;

        private void Awake()
        {
            if (definition == null)
            {
                return;
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (enemyChaser == null)
            {
                enemyChaser = GetComponent<SimpleEnemyChaser>();
            }

            if (contactDamage == null)
            {
                contactDamage = GetComponent<ContactDamage>();
            }

            if (health != null)
            {
                health.SetMaxHealth(definition.MaxHealth, true);
            }

            if (enemyChaser != null)
            {
                enemyChaser.Configure(definition.MoveSpeed, definition.StoppingDistance);
            }

            if (contactDamage != null)
            {
                contactDamage.Configure(definition.ContactDamage);
            }
        }
    }
}
