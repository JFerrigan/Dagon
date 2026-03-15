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

        public void InitializeRuntime(ActiveAbilityDefinition runtimeDefinition, int slotIndex, Camera worldCamera)
        {
            definition = runtimeDefinition;
            SlotIndex = slotIndex;
            ApplyDefinition(runtimeDefinition);
            ConfigureRuntime(worldCamera);
        }

        public abstract float CooldownRemaining { get; }

        public abstract float CooldownDuration { get; }

        public abstract void ConfigureRuntime(Camera worldCamera);

        public abstract void ModifyRadius(float amount);

        protected abstract void ApplyDefinition(ActiveAbilityDefinition runtimeDefinition);
    }
}
