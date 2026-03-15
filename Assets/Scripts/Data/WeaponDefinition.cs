using UnityEngine;

namespace Dagon.Data
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Dagon/Data/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string weaponId = "weapon.harpoon_cast";
        [SerializeField] private string displayName = "Harpoon Cast";
        [SerializeField] [TextArea] private string description = "Launches barbed harpoons in short bursts.";

        [Header("Combat")]
        [SerializeField] private float attacksPerSecond = 2f;
        [SerializeField] private float projectileSpeed = 14f;
        [SerializeField] private float projectileDamage = 1f;
        [SerializeField] private int projectilesPerVolley = 1;
        [SerializeField] private float spreadAngle = 8f;

        public string WeaponId => weaponId;
        public string DisplayName => displayName;
        public string Description => description;
        public float AttacksPerSecond => attacksPerSecond;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileDamage => projectileDamage;
        public int ProjectilesPerVolley => projectilesPerVolley;
        public float SpreadAngle => spreadAngle;
    }
}
