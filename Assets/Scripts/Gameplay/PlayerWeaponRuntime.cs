using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    public abstract class PlayerWeaponRuntime : MonoBehaviour
    {
        protected WeaponDefinition definition;

        public string WeaponId => definition != null ? definition.WeaponId : string.Empty;
        public string DisplayName => definition != null ? definition.DisplayName : name;
        public bool IsBaseWeapon { get; private set; }
        public int Rank { get; private set; }

        public void InitializeRuntime(WeaponDefinition runtimeDefinition, bool isBaseWeapon, Camera worldCamera)
        {
            definition = runtimeDefinition;
            IsBaseWeapon = isBaseWeapon;
            Rank = 1;
            ApplyDefinition(runtimeDefinition);
            ConfigureRuntime(worldCamera);
        }

        public void RankUp()
        {
            Rank += 1;
            ApplyRankBonus(Rank);
        }

        public abstract void ConfigureRuntime(Camera worldCamera);

        public abstract void ModifyAttackRate(float amount);

        public abstract void ModifyProjectileDamage(float amount);

        public abstract void ModifyProjectileCount(int amount);

        protected abstract void ApplyDefinition(WeaponDefinition runtimeDefinition);

        protected abstract void ApplyRankBonus(int currentRank);
    }
}
