using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class WeaponLoadout : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition startingWeapon;
        [SerializeField] private HarpoonLauncher harpoonLauncher;

        private void Awake()
        {
            if (harpoonLauncher == null)
            {
                harpoonLauncher = GetComponent<HarpoonLauncher>();
            }

            ApplyStartingWeapon();
        }

        public void ApplyStartingWeapon()
        {
            if (startingWeapon == null || harpoonLauncher == null)
            {
                return;
            }

            harpoonLauncher.Configure(
                startingWeapon.AttacksPerSecond,
                startingWeapon.ProjectileSpeed,
                startingWeapon.ProjectileDamage,
                startingWeapon.ProjectilesPerVolley,
                startingWeapon.SpreadAngle);
        }
    }
}
