using Dagon.Data;
using UnityEngine;
using System.Text;

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

        protected Vector3 GetProjectileLaunchOrigin(float verticalOffset = 0f)
        {
            var capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                return transform.TransformPoint(capsule.center + new Vector3(0f, verticalOffset, 0f));
            }

            return transform.position + Vector3.up * verticalOffset;
        }

        protected Vector3 GetProjectileLaunchOrigin(Vector3 forwardDirection, float forwardOffset, float verticalOffset = 0f)
        {
            var origin = GetProjectileLaunchOrigin(verticalOffset);
            if (forwardDirection.sqrMagnitude <= 0.001f || Mathf.Abs(forwardOffset) <= Mathf.Epsilon)
            {
                return origin;
            }

            return origin + (forwardDirection.normalized * forwardOffset);
        }

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
            return ReplaceTrailingRomanNumeral(GetUpgradeTitle(path, 3), ToRomanNumeral(nextStep));
        }

        protected virtual string GetOverflowUpgradeDescription(WeaponUpgradePath path, int nextStep)
        {
            return path == WeaponUpgradePath.PathA
                ? "+1 Projectile, +5% Rate"
                : "+0.35 DMG, +2% Rate";
        }

        protected abstract string GetUpgradeTitle(WeaponUpgradePath path, int nextStep);

        protected abstract string GetUpgradeDescription(WeaponUpgradePath path, int nextStep);

        private static string ReplaceTrailingRomanNumeral(string title, string numeral)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return numeral;
            }

            var lastSpaceIndex = title.LastIndexOf(' ');
            if (lastSpaceIndex < 0 || lastSpaceIndex >= title.Length - 1)
            {
                return $"{title} {numeral}";
            }

            return $"{title[..(lastSpaceIndex + 1)]}{numeral}";
        }

        private static string ToRomanNumeral(int value)
        {
            if (value <= 0)
            {
                return "0";
            }

            var builder = new StringBuilder();
            AppendRoman(builder, ref value, 1000, "M");
            AppendRoman(builder, ref value, 900, "CM");
            AppendRoman(builder, ref value, 500, "D");
            AppendRoman(builder, ref value, 400, "CD");
            AppendRoman(builder, ref value, 100, "C");
            AppendRoman(builder, ref value, 90, "XC");
            AppendRoman(builder, ref value, 50, "L");
            AppendRoman(builder, ref value, 40, "XL");
            AppendRoman(builder, ref value, 10, "X");
            AppendRoman(builder, ref value, 9, "IX");
            AppendRoman(builder, ref value, 5, "V");
            AppendRoman(builder, ref value, 4, "IV");
            AppendRoman(builder, ref value, 1, "I");
            return builder.ToString();
        }

        private static void AppendRoman(StringBuilder builder, ref int remaining, int magnitude, string token)
        {
            while (remaining >= magnitude)
            {
                builder.Append(token);
                remaining -= magnitude;
            }
        }
    }
}
