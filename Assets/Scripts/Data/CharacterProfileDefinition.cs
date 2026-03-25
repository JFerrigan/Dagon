using UnityEngine;

namespace Dagon.Data
{
    [CreateAssetMenu(fileName = "CharacterProfileDefinition", menuName = "Dagon/Data/Character Profile Definition")]
    public sealed class CharacterProfileDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string characterId = "character.sailor";
        [SerializeField] private string displayName = "Sailor";
        [SerializeField] [TextArea] private string description = "A mire-tough sailor who survives by keeping the horde at bay.";

        [Header("Visuals")]
        [SerializeField] private string portraitSpritePath = "Sprites/Characters/sailor_idle_front";
        [SerializeField] private string runtimeSpritePath = "Sprites/Characters/sailor_idle_front";
        [SerializeField] private Color accentColor = Color.white;
        [SerializeField] private float runtimeScale = 1f;

        [Header("Traits")]
        [SerializeField] private float maxHealthMultiplier = 1f;
        [SerializeField] private float moveSpeedMultiplier = 1f;
        [SerializeField] private float corruptionGainMultiplier = 1f;
        [SerializeField] private string traitSummary = string.Empty;

        [Header("Starting Kit")]
        [SerializeField] private WeaponDefinition startingBaseWeapon;
        [SerializeField] private ActiveAbilityDefinition startingActive;

        public string CharacterId => characterId;
        public string DisplayName => displayName;
        public string Description => description;
        public string PortraitSpritePath => portraitSpritePath;
        public string RuntimeSpritePath => runtimeSpritePath;
        public Color AccentColor => accentColor;
        public float RuntimeScale => runtimeScale;
        public float MaxHealthMultiplier => maxHealthMultiplier;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        public float CorruptionGainMultiplier => corruptionGainMultiplier;
        public string TraitSummary => traitSummary;
        public WeaponDefinition StartingBaseWeapon => startingBaseWeapon;
        public ActiveAbilityDefinition StartingActive => startingActive;

        public static CharacterProfileDefinition CreateRuntime(
            string id,
            string name,
            string details,
            string portraitPath,
            string runtimePath,
            Color accent,
            float scale,
            float healthMultiplier,
            float speedMultiplier,
            float corruptionMultiplier,
            string traits,
            WeaponDefinition baseWeapon,
            ActiveAbilityDefinition active)
        {
            var profile = CreateInstance<CharacterProfileDefinition>();
            profile.characterId = id;
            profile.displayName = name;
            profile.description = details;
            profile.portraitSpritePath = portraitPath;
            profile.runtimeSpritePath = runtimePath;
            profile.accentColor = accent;
            profile.runtimeScale = Mathf.Max(0.05f, scale);
            profile.maxHealthMultiplier = Mathf.Max(0.1f, healthMultiplier);
            profile.moveSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            profile.corruptionGainMultiplier = Mathf.Max(0f, corruptionMultiplier);
            profile.traitSummary = traits ?? string.Empty;
            profile.startingBaseWeapon = baseWeapon;
            profile.startingActive = active;
            return profile;
        }
    }
}
