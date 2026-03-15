using UnityEngine;

namespace Dagon.Data
{
    [CreateAssetMenu(fileName = "CharacterLoadoutDefinition", menuName = "Dagon/Data/Character Loadout Definition")]
    public sealed class CharacterLoadoutDefinition : ScriptableObject
    {
        [SerializeField] private WeaponDefinition startingBaseWeapon;
        [SerializeField] private ActiveAbilityDefinition[] startingActives;

        public WeaponDefinition StartingBaseWeapon => startingBaseWeapon;
        public ActiveAbilityDefinition[] StartingActives => startingActives;

        public static CharacterLoadoutDefinition CreateRuntime(
            WeaponDefinition baseWeapon,
            params ActiveAbilityDefinition[] actives)
        {
            var definition = CreateInstance<CharacterLoadoutDefinition>();
            definition.startingBaseWeapon = baseWeapon;
            definition.startingActives = actives ?? System.Array.Empty<ActiveAbilityDefinition>();
            return definition;
        }
    }
}
