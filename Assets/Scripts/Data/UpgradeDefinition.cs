using UnityEngine;

namespace Dagon.Data
{
    public enum UpgradeStat
    {
        AttacksPerSecond,
        ProjectileDamage,
        ProjectileSpeed,
        ProjectilesPerVolley,
        SpreadAngle,
        MaxHealth,
        CorruptionGain
    }

    [CreateAssetMenu(fileName = "UpgradeDefinition", menuName = "Dagon/Data/Upgrade Definition")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string upgradeId = "upgrade.barbed_rigging";
        [SerializeField] private string displayName = "Barbed Rigging";
        [SerializeField] [TextArea] private string description = "Your harpoons pull more flesh from what they strike.";

        [Header("Effect")]
        [SerializeField] private UpgradeStat targetStat = UpgradeStat.ProjectileDamage;
        [SerializeField] private float additiveValue = 0.5f;
        [SerializeField] private float multiplicativeValue = 1f;

        public string UpgradeId => upgradeId;
        public string DisplayName => displayName;
        public string Description => description;
        public UpgradeStat TargetStat => targetStat;
        public float AdditiveValue => additiveValue;
        public float MultiplicativeValue => multiplicativeValue;
    }
}
