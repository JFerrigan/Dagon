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
        private float globalAttackRateModifier;
        private float globalProjectileDamageModifier;
        private float globalActiveCooldownMultiplier = 1f;

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
                return existing;
            }

            var runtime = CreateWeaponRuntime(definition);
            runtime.InitializeRuntime(definition, isBaseWeapon, worldCamera);
            if (Mathf.Abs(globalAttackRateModifier) > 0.0001f)
            {
                runtime.ModifyAttackRate(globalAttackRateModifier);
            }

            if (Mathf.Abs(globalProjectileDamageModifier) > 0.0001f)
            {
                runtime.ModifyProjectileDamage(globalProjectileDamageModifier);
            }

            weapons.Add(runtime);
            return runtime;
        }

        public void ResetWeaponsToStartingLoadout()
        {
            ClearWeapons();
            if (startingLoadout?.StartingBaseWeapon != null)
            {
                AddWeapon(startingLoadout.StartingBaseWeapon, true);
            }
        }

        public void ResetActivesToStartingLoadout()
        {
            ClearActives();
            if (startingLoadout?.StartingActives == null)
            {
                return;
            }

            for (var slotIndex = 0; slotIndex < startingLoadout.StartingActives.Length; slotIndex++)
            {
                if (startingLoadout.StartingActives[slotIndex] == null)
                {
                    continue;
                }

                EquipActive(slotIndex, startingLoadout.StartingActives[slotIndex]);
            }
        }

        public void ResetToStartingLoadout()
        {
            ResetWeaponsToStartingLoadout();
            ResetActivesToStartingLoadout();
        }

        public void SetWeapons(IEnumerable<WeaponDefinition> definitions, string baseWeaponId = null)
        {
            ClearWeapons();
            if (definitions == null)
            {
                ResetWeaponsToStartingLoadout();
                return;
            }

            var uniqueDefinitions = definitions
                .Where(definition => definition != null)
                .GroupBy(definition => definition.WeaponId)
                .Select(group => group.First())
                .ToList();

            if (uniqueDefinitions.Count == 0)
            {
                ResetWeaponsToStartingLoadout();
                return;
            }

            var resolvedBaseWeaponId = !string.IsNullOrWhiteSpace(baseWeaponId)
                ? baseWeaponId
                : uniqueDefinitions[0].WeaponId;

            for (var index = 0; index < uniqueDefinitions.Count; index++)
            {
                var definition = uniqueDefinitions[index];
                var isBaseWeapon = definition.WeaponId == resolvedBaseWeaponId;
                AddWeapon(definition, isBaseWeapon);
            }

            if (BaseWeapon == null && weapons.Count > 0)
            {
                ClearWeapons();
                AddWeapon(uniqueDefinitions[0], true);
                for (var index = 1; index < uniqueDefinitions.Count; index++)
                {
                    AddWeapon(uniqueDefinitions[index], false);
                }
            }
        }

        public bool UpgradeWeapon(string weaponId, WeaponUpgradePath path)
        {
            var weapon = GetWeapon(weaponId);
            if (weapon == null)
            {
                return false;
            }

            return weapon.ApplyPathUpgrade(path);
        }

        public void ModifyAllWeaponsAttackRate(float amount)
        {
            globalAttackRateModifier += amount;
            foreach (var weapon in weapons)
            {
                weapon.ModifyAttackRate(amount);
            }
        }

        public void ModifyAllWeaponsDamage(float amount)
        {
            globalProjectileDamageModifier += amount;
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

        public ActiveAbilityRuntime GetPrimaryActive()
        {
            return GetActive(0);
        }

        public bool UpgradeActive(string abilityId)
        {
            for (var index = 0; index < activeSlots.Count; index++)
            {
                var active = activeSlots[index];
                if (active == null || active.AbilityId != abilityId)
                {
                    continue;
                }

                return active.ApplyUpgrade();
            }

            return false;
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
            runtime.SetCooldownMultiplier(globalActiveCooldownMultiplier);
            activeSlots[slotIndex] = runtime;
            return runtime;
        }

        public ActiveAbilityRuntime ReplacePrimaryActive(ActiveAbilityDefinition definition, bool preserveRank = true)
        {
            if (definition == null)
            {
                return null;
            }

            var previousActive = GetPrimaryActive();
            var targetRank = preserveRank && previousActive != null ? Mathf.Max(1, previousActive.Rank) : 1;
            var runtime = EquipActive(0, definition);
            if (runtime == null)
            {
                return null;
            }

            while (runtime.Rank < targetRank)
            {
                runtime.ApplyUpgrade();
            }

            return runtime;
        }

        public void SetAllActiveCooldownMultiplier(float multiplier)
        {
            globalActiveCooldownMultiplier = Mathf.Max(0.1f, multiplier);
            for (var index = 0; index < activeSlots.Count; index++)
            {
                activeSlots[index]?.SetCooldownMultiplier(globalActiveCooldownMultiplier);
            }
        }

        private void ClearWeapons()
        {
            for (var index = weapons.Count - 1; index >= 0; index--)
            {
                if (weapons[index] != null)
                {
                    Destroy(weapons[index]);
                }
            }

            weapons.Clear();
        }

        private void ClearActives()
        {
            for (var index = activeSlots.Count - 1; index >= 0; index--)
            {
                if (activeSlots[index] != null)
                {
                    Destroy(activeSlots[index]);
                }
            }

            activeSlots.Clear();
        }

        private PlayerWeaponRuntime CreateWeaponRuntime(WeaponDefinition definition)
        {
            return definition.RuntimeKind switch
            {
                WeaponRuntimeKind.ProjectileLauncher => gameObject.AddComponent<HarpoonLauncher>(),
                WeaponRuntimeKind.AnchorChain => gameObject.AddComponent<AnchorChainWeapon>(),
                WeaponRuntimeKind.RotLantern => gameObject.AddComponent<RotLanternWeapon>(),
                WeaponRuntimeKind.BilgeSpray => gameObject.AddComponent<BilgeSprayWeapon>(),
                WeaponRuntimeKind.RotBeaconBomb => gameObject.AddComponent<RotBeaconBombWeapon>(),
                WeaponRuntimeKind.Floodline => gameObject.AddComponent<FloodlineWeapon>(),
                WeaponRuntimeKind.Tideburst => gameObject.AddComponent<TideburstWeapon>(),
                _ => gameObject.AddComponent<HarpoonLauncher>()
            };
        }

        private ActiveAbilityRuntime CreateActiveRuntime(ActiveAbilityDefinition definition)
        {
            return definition.RuntimeKind switch
            {
                ActiveAbilityRuntimeKind.BrineSurge => gameObject.AddComponent<BrineSurgeAbility>(),
                ActiveAbilityRuntimeKind.Dash => gameObject.AddComponent<DashAbility>(),
                ActiveAbilityRuntimeKind.Frenzy => gameObject.AddComponent<FrenzyAbility>(),
                ActiveAbilityRuntimeKind.AbyssalRebirth => gameObject.AddComponent<AbyssalRebirthAbility>(),
                ActiveAbilityRuntimeKind.BloodwakeStep => gameObject.AddComponent<BloodwakeStepAbility>(),
                ActiveAbilityRuntimeKind.Riftheart => gameObject.AddComponent<RiftheartAbility>(),
                _ => gameObject.AddComponent<BrineSurgeAbility>()
            };
        }
    }
}
