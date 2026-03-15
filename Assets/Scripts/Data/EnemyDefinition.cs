using UnityEngine;

namespace Dagon.Data
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Dagon/Data/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string enemyId = "enemy.mire_wretch";
        [SerializeField] private string displayName = "Mire Wretch";
        [SerializeField] [TextArea] private string description = "A drowned thing dragged half-back into motion by the mire.";

        [Header("Stats")]
        [SerializeField] private float maxHealth = 4f;
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float contactDamage = 1f;
        [SerializeField] private float stoppingDistance = 0.8f;
        [SerializeField] private float corruptionOnKill = 2f;

        public string EnemyId => enemyId;
        public string DisplayName => displayName;
        public string Description => description;
        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float ContactDamage => contactDamage;
        public float StoppingDistance => stoppingDistance;
        public float CorruptionOnKill => corruptionOnKill;
    }
}
