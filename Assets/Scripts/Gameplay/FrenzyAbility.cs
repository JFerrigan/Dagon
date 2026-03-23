using System.Collections.Generic;
using Dagon.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dagon.Gameplay
{
    public sealed class FrenzyAbility : ActiveAbilityRuntime
    {
        [SerializeField] private float cooldown = 14f;
        [SerializeField] private float duration = 4f;
        [SerializeField] private float attackSpeedMultiplier = 2f;

        private readonly Dictionary<PlayerWeaponRuntime, float> appliedBonuses = new();
        private PlayerCombatLoadout combatLoadout;
        private float cooldownRemaining;
        private float activeRemaining;

        public override float CooldownRemaining => cooldownRemaining;
        public override float CooldownDuration => cooldown;

        private void Awake()
        {
            combatLoadout = GetComponent<PlayerCombatLoadout>();
        }

        private void Update()
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.unscaledDeltaTime);
            if (activeRemaining > 0f)
            {
                activeRemaining = Mathf.Max(0f, activeRemaining - Time.unscaledDeltaTime);
                if (activeRemaining <= 0f)
                {
                    RemoveBuff();
                }
            }

            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TryActivate();
            }
        }

        private void OnDisable()
        {
            RemoveBuff();
        }

        public override void ConfigureRuntime(Camera worldCamera)
        {
        }

        public override void ModifyRadius(float amount)
        {
            duration = Mathf.Max(1f, duration + (amount * 0.15f));
        }

        protected override void ApplyDefinition(ActiveAbilityDefinition runtimeDefinition)
        {
            cooldown = Mathf.Max(0.5f, runtimeDefinition.Cooldown);
            duration = Mathf.Max(0.5f, runtimeDefinition.Duration);
            attackSpeedMultiplier = Mathf.Max(1.1f, runtimeDefinition.Magnitude);
        }

        protected override void ApplyUpgrade(int nextRank)
        {
            switch (nextRank)
            {
                case 2:
                    attackSpeedMultiplier += 0.25f;
                    break;
                case 3:
                    attackSpeedMultiplier += 0.35f;
                    break;
                case 4:
                    attackSpeedMultiplier += 0.4f;
                    break;
                case 5:
                    duration += 0.8f;
                    break;
                default:
                    attackSpeedMultiplier += 0.1f;
                    duration += 0.15f;
                    break;
            }
        }

        protected override string GetUpgradeTitle(int nextRank)
        {
            return nextRank switch
            {
                2 => "Frenzy Tempo I",
                3 => "Frenzy Tempo II",
                4 => "Frenzy Tempo III",
                5 => "Frenzy Hold",
                _ => $"Frenzy +{nextRank - 1}"
            };
        }

        protected override string GetUpgradeDescription(int nextRank)
        {
            return nextRank switch
            {
                2 => "+0.25x Attack Speed",
                3 => "+0.35x Attack Speed",
                4 => "+0.40x Attack Speed",
                5 => "+0.8s Frenzy Duration",
                _ => "+0.1x Attack Speed, +0.15s Duration"
            };
        }

        private void TryActivate()
        {
            if (cooldownRemaining > 0f || combatLoadout == null)
            {
                return;
            }

            ApplyBuff();
            activeRemaining = duration;
            cooldownRemaining = cooldown;

            PlaceholderWeaponVisual.Spawn(
                "FrenzyBurst",
                transform.position + Vector3.up * 0.05f,
                new Vector3(3.4f, 3.4f, 1f),
                Camera.main,
                new Color(0.96f, 0.84f, 0.54f, 0.38f),
                0.28f,
                1.1f,
                groundPlane: true);
        }

        private void ApplyBuff()
        {
            RemoveBuff();
            for (var i = 0; i < combatLoadout.Weapons.Count; i++)
            {
                var weapon = combatLoadout.Weapons[i];
                if (weapon == null)
                {
                    continue;
                }

                var currentRate = Mathf.Max(0.1f, weapon.GetAttackRateEstimate());
                var bonus = currentRate * (attackSpeedMultiplier - 1f);
                if (bonus <= 0f)
                {
                    continue;
                }

                weapon.ModifyAttackRate(bonus);
                appliedBonuses[weapon] = bonus;
            }
        }

        private void RemoveBuff()
        {
            foreach (var pair in appliedBonuses)
            {
                if (pair.Key != null)
                {
                    pair.Key.ModifyAttackRate(-pair.Value);
                }
            }

            appliedBonuses.Clear();
            activeRemaining = 0f;
        }
    }
}
