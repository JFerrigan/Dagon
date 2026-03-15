using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class WeaponLoadout : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition startingWeapon;
        [SerializeField] private PlayerCombatLoadout combatLoadout;

        private void Awake()
        {
            if (combatLoadout == null)
            {
                combatLoadout = GetComponent<PlayerCombatLoadout>();
            }

            ApplyStartingWeapon();
        }

        public void ApplyStartingWeapon()
        {
            if (startingWeapon == null || combatLoadout == null)
            {
                return;
            }

            if (!combatLoadout.HasWeapon(startingWeapon.WeaponId))
            {
                combatLoadout.AddWeapon(startingWeapon, true);
            }
        }
    }
}
