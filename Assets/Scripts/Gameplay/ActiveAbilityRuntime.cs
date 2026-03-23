using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    public abstract class ActiveAbilityRuntime : MonoBehaviour
    {
        protected ActiveAbilityDefinition definition;

        public string AbilityId => definition != null ? definition.AbilityId : string.Empty;
        public string DisplayName => definition != null ? definition.DisplayName : name;
        public int SlotIndex { get; private set; }
        public int Rank { get; private set; } = 1;

        public void InitializeRuntime(ActiveAbilityDefinition runtimeDefinition, int slotIndex, Camera worldCamera)
        {
            definition = runtimeDefinition;
            SlotIndex = slotIndex;
            Rank = 1;
            ApplyDefinition(runtimeDefinition);
            ConfigureRuntime(worldCamera);
        }

        public bool ApplyUpgrade()
        {
            var nextRank = Rank + 1;
            ApplyUpgrade(nextRank);
            Rank = nextRank;
            return true;
        }

        public bool TryBuildUpgradeReward(out CombatRewardOption reward)
        {
            reward = default;
            if (string.IsNullOrWhiteSpace(AbilityId))
            {
                return false;
            }

            var nextRank = Rank + 1;
            reward = new CombatRewardOption(
                CombatRewardKind.UpgradeActiveAbility,
                $"{DisplayName} - {GetUpgradeTitle(nextRank)}",
                GetUpgradeDescription(nextRank),
                targetAbilityId: AbilityId);
            return true;
        }

        public abstract float CooldownRemaining { get; }

        public abstract float CooldownDuration { get; }

        public abstract void ConfigureRuntime(Camera worldCamera);

        public abstract void ModifyRadius(float amount);

        protected abstract void ApplyDefinition(ActiveAbilityDefinition runtimeDefinition);

        protected abstract void ApplyUpgrade(int nextRank);

        protected abstract string GetUpgradeTitle(int nextRank);

        protected abstract string GetUpgradeDescription(int nextRank);
    }
}
