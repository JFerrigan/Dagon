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
            return definition != null || !string.IsNullOrEmpty(name);
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
            if (nextStep <= 3)
            {
                ApplyPathUpgrade(path, nextStep);
            }
            else
            {
                ApplyOverflowUpgrade(path, nextStep);
            }

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
                $"{DisplayName} - {(step <= 3 ? GetUpgradeTitle(path, step) : GetOverflowUpgradeTitle(path, step))}",
                step <= 3 ? GetUpgradeDescription(path, step) : GetOverflowUpgradeDescription(path, step),
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

        public abstract float GetAttackRateEstimate();

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

        protected virtual void ApplyOverflowUpgrade(WeaponUpgradePath path, int nextStep)
        {
            if (path == WeaponUpgradePath.PathA)
            {
                ModifyProjectileCount(1);
                ModifyAttackRate(0.05f);
                return;
            }

            ModifyProjectileDamage(0.35f);
            ModifyAttackRate(0.02f);
        }

        protected virtual string GetOverflowUpgradeTitle(WeaponUpgradePath path, int nextStep)
        {
            return $"{GetPathName(path)} +{nextStep - 3}";
        }

        protected virtual string GetOverflowUpgradeDescription(WeaponUpgradePath path, int nextStep)
        {
            return path == WeaponUpgradePath.PathA
                ? "Bonus Path A"
                : "Bonus Path B";
        }

        protected abstract string GetUpgradeTitle(WeaponUpgradePath path, int nextStep);

        protected abstract string GetUpgradeDescription(WeaponUpgradePath path, int nextStep);
    }
}
