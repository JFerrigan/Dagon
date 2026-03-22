using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    public abstract class PlayerWeaponRuntime : MonoBehaviour
    {
        protected WeaponDefinition definition;
        private int pathAUpgradesTaken;
        private int pathBUpgradesTaken;

        public WeaponDefinition Definition => definition;
        public string WeaponId => definition != null ? definition.WeaponId : string.Empty;
        public string DisplayName => definition != null ? definition.DisplayName : name;
        public bool IsBaseWeapon { get; private set; }
        public int Rank => 1 + pathAUpgradesTaken + pathBUpgradesTaken;
        public int PathAUpgradesTaken => pathAUpgradesTaken;
        public int PathBUpgradesTaken => pathBUpgradesTaken;
        public abstract string PathAName { get; }
        public abstract string PathBName { get; }

        public void InitializeRuntime(WeaponDefinition runtimeDefinition, bool isBaseWeapon, Camera worldCamera)
        {
            definition = runtimeDefinition;
            IsBaseWeapon = isBaseWeapon;
            pathAUpgradesTaken = 0;
            pathBUpgradesTaken = 0;
            ApplyDefinition(runtimeDefinition);
            ConfigureRuntime(worldCamera);
        }

        public bool CanUpgradePath(WeaponUpgradePath path)
        {
            return GetPathUpgradesTaken(path) < 3;
        }

        public int GetPathUpgradesTaken(WeaponUpgradePath path)
        {
            return path == WeaponUpgradePath.PathA ? pathAUpgradesTaken : pathBUpgradesTaken;
        }

        public float GetPathSelectionWeight(WeaponUpgradePath path)
        {
            if (!CanUpgradePath(path))
            {
                return 0f;
            }

            if (pathAUpgradesTaken == pathBUpgradesTaken)
            {
                return 5f;
            }

            if (path == WeaponUpgradePath.PathA)
            {
                return pathAUpgradesTaken > pathBUpgradesTaken ? 6f : 4f;
            }

            return pathBUpgradesTaken > pathAUpgradesTaken ? 6f : 4f;
        }

        public bool ApplyPathUpgrade(WeaponUpgradePath path)
        {
            if (!CanUpgradePath(path))
            {
                return false;
            }

            var nextStep = GetPathUpgradesTaken(path) + 1;
            ApplyPathUpgrade(path, nextStep);
            if (path == WeaponUpgradePath.PathA)
            {
                pathAUpgradesTaken = nextStep;
            }
            else
            {
                pathBUpgradesTaken = nextStep;
            }

            return true;
        }

        public bool ApplySandboxPathUpgrade(WeaponUpgradePath path)
        {
            return ApplyPathUpgrade(path);
        }

        public bool TryBuildPathReward(WeaponUpgradePath path, out CombatRewardOption reward)
        {
            reward = default;
            if (!CanUpgradePath(path))
            {
                return false;
            }

            var step = GetPathUpgradesTaken(path) + 1;
            reward = new CombatRewardOption(
                CombatRewardKind.UpgradeWeaponPath,
                $"{DisplayName} - {GetUpgradeTitle(path, step)}",
                GetUpgradeDescription(path, step),
                targetWeaponId: WeaponId,
                upgradePath: path);
            return true;
        }

        public string GetPathName(WeaponUpgradePath path)
        {
            return path == WeaponUpgradePath.PathA ? PathAName : PathBName;
        }

        public abstract void ConfigureRuntime(Camera worldCamera);

        public abstract void ModifyAttackRate(float amount);

        public abstract void ModifyProjectileDamage(float amount);

        public abstract void ModifyProjectileCount(int amount);

        protected abstract void ApplyDefinition(WeaponDefinition runtimeDefinition);

        protected abstract void ApplyPathUpgrade(WeaponUpgradePath path, int nextStep);

        protected static string FlatDamageDelta(float damageDelta)
        {
            return $"+{Mathf.RoundToInt(damageDelta * 10f)} DMG";
        }

        protected static string FlatCountDelta(int amount, string label)
        {
            return $"+{amount} {label}";
        }

        protected static string FlatPercentDelta(int amount, string label)
        {
            return $"+{amount}% {label}";
        }

        protected abstract string GetUpgradeTitle(WeaponUpgradePath path, int nextStep);

        protected abstract string GetUpgradeDescription(WeaponUpgradePath path, int nextStep);
    }
}
