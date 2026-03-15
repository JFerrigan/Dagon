using Dagon.Data;

namespace Dagon.Gameplay
{
    public enum CombatRewardKind
    {
        GlobalAttackRate,
        GlobalProjectileDamage,
        GlobalProjectileCount,
        AcquireWeapon,
        UpgradeWeaponAttackRate,
        UpgradeWeaponDamage,
        UpgradeWeaponProjectileCount,
        ActiveRadius,
        MaxHealth,
        CorruptionPulse
    }

    public readonly struct CombatRewardOption
    {
        public CombatRewardOption(
            CombatRewardKind kind,
            string title,
            string description,
            WeaponDefinition weaponDefinition = null,
            string targetWeaponId = null)
        {
            Kind = kind;
            Title = title;
            Description = description;
            WeaponDefinition = weaponDefinition;
            TargetWeaponId = targetWeaponId ?? string.Empty;
        }

        public CombatRewardKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
        public WeaponDefinition WeaponDefinition { get; }
        public string TargetWeaponId { get; }
    }

    public readonly struct CombatRewardChoiceSet
    {
        public CombatRewardChoiceSet(CombatRewardOption[] options)
        {
            Options = options;
        }

        public CombatRewardOption[] Options { get; }
    }
}
