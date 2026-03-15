using System.Collections.Generic;
using System.Linq;
using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerCombatLoadout : MonoBehaviour
    {
        [SerializeField] private CharacterLoadoutDefinition startingLoadout;
        [SerializeField] private Camera worldCamera;

        private readonly List<PlayerWeaponRuntime> weapons = new();
        private readonly List<WeaponDefinition> weaponPool = new();
        private readonly List<ActiveAbilityRuntime> activeSlots = new();

        public IReadOnlyList<PlayerWeaponRuntime> Weapons => weapons;
        public IReadOnlyList<WeaponDefinition> WeaponPool => weaponPool;

        public PlayerWeaponRuntime BaseWeapon => weapons.FirstOrDefault(weapon => weapon.IsBaseWeapon);

        public void ConfigureRuntime(Camera cameraReference)
        {
            worldCamera = cameraReference;

            foreach (var weapon in weapons)
            {
                weapon.ConfigureRuntime(worldCamera);
            }

            foreach (var active in activeSlots)
            {
                active?.ConfigureRuntime(worldCamera);
            }
        }

        public void SetWeaponPool(IEnumerable<WeaponDefinition> definitions)
        {
            weaponPool.Clear();
            if (definitions == null)
            {
                return;
            }

            foreach (var definition in definitions)
            {
                if (definition == null || weaponPool.Any(existing => existing.WeaponId == definition.WeaponId))
                {
                    continue;
                }

                weaponPool.Add(definition);
            }
        }

        public void Initialize(CharacterLoadoutDefinition loadoutDefinition)
        {
            startingLoadout = loadoutDefinition;
            if (startingLoadout == null)
            {
                return;
            }

            if (weapons.Count == 0 && startingLoadout.StartingBaseWeapon != null)
            {
                AddWeapon(startingLoadout.StartingBaseWeapon, true);
            }

            if (activeSlots.Count == 0 && startingLoadout.StartingActives != null)
            {
                for (var slotIndex = 0; slotIndex < startingLoadout.StartingActives.Length; slotIndex++)
                {
                    if (startingLoadout.StartingActives[slotIndex] == null)
                    {
                        continue;
                    }

                    EquipActive(slotIndex, startingLoadout.StartingActives[slotIndex]);
                }
            }
        }

        public bool HasWeapon(string weaponId)
        {
            return weapons.Any(weapon => weapon.WeaponId == weaponId);
        }

        public PlayerWeaponRuntime GetWeapon(string weaponId)
        {
            return weapons.FirstOrDefault(weapon => weapon.WeaponId == weaponId);
        }

        public IEnumerable<WeaponDefinition> GetAvailableWeaponOffers()
        {
            return weaponPool.Where(definition => definition != null && !HasWeapon(definition.WeaponId));
        }

        public PlayerWeaponRuntime AddWeapon(WeaponDefinition definition, bool isBaseWeapon = false)
        {
            if (definition == null)
            {
                return null;
            }

            var existing = GetWeapon(definition.WeaponId);
            if (existing != null)
            {
                existing.RankUp();
                return existing;
            }

            var runtime = CreateWeaponRuntime(definition);
            runtime.InitializeRuntime(definition, isBaseWeapon, worldCamera);
            weapons.Add(runtime);
            return runtime;
        }

        public bool UpgradeWeapon(string weaponId, CombatRewardKind rewardKind)
        {
            var weapon = GetWeapon(weaponId);
            if (weapon == null)
            {
                return false;
            }

            switch (rewardKind)
            {
                case CombatRewardKind.UpgradeWeaponAttackRate:
                    weapon.ModifyAttackRate(0.2f);
                    return true;
                case CombatRewardKind.UpgradeWeaponDamage:
                    weapon.ModifyProjectileDamage(0.5f);
                    return true;
                case CombatRewardKind.UpgradeWeaponProjectileCount:
                    weapon.ModifyProjectileCount(1);
                    return true;
                default:
                    return false;
            }
        }

        public void ModifyAllWeaponsAttackRate(float amount)
        {
            foreach (var weapon in weapons)
            {
                weapon.ModifyAttackRate(amount);
            }
        }

        public void ModifyAllWeaponsDamage(float amount)
        {
            foreach (var weapon in weapons)
            {
                weapon.ModifyProjectileDamage(amount);
            }
        }

        public void ModifyAllWeaponsProjectileCount(int amount)
        {
            foreach (var weapon in weapons)
            {
                weapon.ModifyProjectileCount(amount);
            }
        }

        public ActiveAbilityRuntime GetActive(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= activeSlots.Count)
            {
                return null;
            }

            return activeSlots[slotIndex];
        }

        public T GetActive<T>(int slotIndex) where T : ActiveAbilityRuntime
        {
            return GetActive(slotIndex) as T;
        }

        public ActiveAbilityRuntime EquipActive(int slotIndex, ActiveAbilityDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            while (activeSlots.Count <= slotIndex)
            {
                activeSlots.Add(null);
            }

            var existing = activeSlots[slotIndex];
            if (existing != null)
            {
                Destroy(existing);
                activeSlots[slotIndex] = null;
            }

            var runtime = CreateActiveRuntime(definition);
            runtime.InitializeRuntime(definition, slotIndex, worldCamera);
            activeSlots[slotIndex] = runtime;
            return runtime;
        }

        private PlayerWeaponRuntime CreateWeaponRuntime(WeaponDefinition definition)
        {
            return definition.RuntimeKind switch
            {
                WeaponRuntimeKind.ProjectileLauncher => gameObject.AddComponent<HarpoonLauncher>(),
                _ => gameObject.AddComponent<HarpoonLauncher>()
            };
        }

        private ActiveAbilityRuntime CreateActiveRuntime(ActiveAbilityDefinition definition)
        {
            return definition.RuntimeKind switch
            {
                ActiveAbilityRuntimeKind.BrineSurge => gameObject.AddComponent<BrineSurgeAbility>(),
                _ => gameObject.AddComponent<BrineSurgeAbility>()
            };
        }
    }
}
