using UnityEngine;

namespace Dagon.Data
{
    [CreateAssetMenu(fileName = "ActiveAbilityDefinition", menuName = "Dagon/Data/Active Ability Definition")]
    public sealed class ActiveAbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string abilityId = "ability.brine_surge";
        [SerializeField] private string displayName = "Brine Surge";
        [SerializeField] [TextArea] private string description = "A black-water burst that punishes crowd pressure.";

        [Header("Runtime")]
        [SerializeField] private ActiveAbilityRuntimeKind runtimeKind = ActiveAbilityRuntimeKind.BrineSurge;

        [Header("Combat")]
        [SerializeField] private float cooldown = 6f;
        [SerializeField] private float radius = 2.8f;
        [SerializeField] private float damage = 2f;
        [SerializeField] private float duration = 0f;
        [SerializeField] private float magnitude = 0f;
        [SerializeField] private int charges = 1;

        public string AbilityId => abilityId;
        public string DisplayName => displayName;
        public string Description => description;
        public ActiveAbilityRuntimeKind RuntimeKind => runtimeKind;
        public float Cooldown => cooldown;
        public float Radius => radius;
        public float Damage => damage;
        public float Duration => duration;
        public float Magnitude => magnitude;
        public int Charges => charges;

        public static ActiveAbilityDefinition CreateRuntime(
            string id,
            string name,
            string details,
            ActiveAbilityRuntimeKind kind,
            float cooldownSeconds,
            float radiusAmount,
            float damageAmount,
            float durationSeconds = 0f,
            float magnitude = 0f,
            int charges = 1)
        {
            var definition = CreateInstance<ActiveAbilityDefinition>();
            definition.abilityId = id;
            definition.displayName = name;
            definition.description = details;
            definition.runtimeKind = kind;
            definition.cooldown = cooldownSeconds;
            definition.radius = radiusAmount;
            definition.damage = damageAmount;
            definition.duration = durationSeconds;
            definition.magnitude = magnitude;
            definition.charges = Mathf.Max(1, charges);
            return definition;
        }
    }
}
