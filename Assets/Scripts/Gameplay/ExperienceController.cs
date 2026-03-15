using System;
using System.Collections.Generic;
using System.Linq;
using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ExperienceController : MonoBehaviour
    {
        [SerializeField] private int startingLevel = 1;
        [SerializeField] private int baseXpRequirement = 5;
        [SerializeField] private int xpRequirementGrowth = 3;

        private readonly Queue<CombatRewardChoiceSet> queuedChoices = new();
        private PlayerCombatLoadout combatLoadout;
        private Health health;
        private CorruptionMeter corruptionMeter;

        public int Level { get; private set; }
        public int CurrentXp { get; private set; }
        public int RequiredXp { get; private set; }
        public bool HasPendingChoice => queuedChoices.Count > 0;

        public event Action Changed;

        private void Awake()
        {
            combatLoadout = GetComponent<PlayerCombatLoadout>();
            health = GetComponent<Health>();
            corruptionMeter = GetComponent<CorruptionMeter>();

            Level = Mathf.Max(1, startingLevel);
            RequiredXp = GetRequiredXpForLevel(Level);
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentXp += amount;
            while (CurrentXp >= RequiredXp)
            {
                CurrentXp -= RequiredXp;
                Level += 1;
                RequiredXp = GetRequiredXpForLevel(Level);
                queuedChoices.Enqueue(BuildChoiceSet());
            }

            Changed?.Invoke();
        }

        public CombatRewardChoiceSet PeekChoices()
        {
            return queuedChoices.Count > 0 ? queuedChoices.Peek() : default;
        }

        public void ApplyChoice(int index)
        {
            if (queuedChoices.Count == 0)
            {
                return;
            }

            var choices = queuedChoices.Dequeue();
            if (index < 0 || index >= choices.Options.Length)
            {
                index = 0;
            }

            ApplyReward(choices.Options[index]);
            Changed?.Invoke();
        }

        private int GetRequiredXpForLevel(int level)
        {
            return baseXpRequirement + ((level - 1) * xpRequirementGrowth);
        }

        private CombatRewardChoiceSet BuildChoiceSet()
        {
            var candidates = new List<CombatRewardOption>();
            if (combatLoadout != null)
            {
                candidates.Add(new CombatRewardOption(
                    CombatRewardKind.GlobalAttackRate,
                    "Tighter Rhythm",
                    "All owned weapons fire faster."));
                candidates.Add(new CombatRewardOption(
                    CombatRewardKind.GlobalProjectileDamage,
                    "Barbed Iron",
                    "All owned weapons hit harder."));

                if (combatLoadout.Weapons.Count > 0)
                {
                    candidates.Add(new CombatRewardOption(
                        CombatRewardKind.GlobalProjectileCount,
                        "Storm Rack",
                        "All owned weapons fire one additional projectile when possible."));
                }

                foreach (var weapon in combatLoadout.Weapons)
                {
                    candidates.Add(new CombatRewardOption(
                        CombatRewardKind.UpgradeWeaponDamage,
                        $"Hone {weapon.DisplayName}",
                        $"{weapon.DisplayName} deals more damage.",
                        targetWeaponId: weapon.WeaponId));
                    candidates.Add(new CombatRewardOption(
                        CombatRewardKind.UpgradeWeaponAttackRate,
                        $"Quicken {weapon.DisplayName}",
                        $"{weapon.DisplayName} attacks faster.",
                        targetWeaponId: weapon.WeaponId));
                }

                foreach (var definition in combatLoadout.GetAvailableWeaponOffers().Take(2))
                {
                    candidates.Add(new CombatRewardOption(
                        CombatRewardKind.AcquireWeapon,
                        $"Claim {definition.DisplayName}",
                        definition.Description,
                        definition));
                }

                if (combatLoadout.GetActive(0) != null)
                {
                    candidates.Add(new CombatRewardOption(
                        CombatRewardKind.ActiveRadius,
                        "Rising Tide",
                        "Increase the area of your equipped active ability."));
                }
            }

            candidates.Add(new CombatRewardOption(
                CombatRewardKind.MaxHealth,
                "Salt-Hardened",
                "Increase max health."));
            candidates.Add(new CombatRewardOption(
                CombatRewardKind.CorruptionPulse,
                "Tide of Dagon",
                "Gain corruption and empower the run."));

            var selected = new List<CombatRewardOption>();
            while (selected.Count < 3 && candidates.Count > 0)
            {
                var index = UnityEngine.Random.Range(0, candidates.Count);
                selected.Add(candidates[index]);
                candidates.RemoveAt(index);
            }

            return new CombatRewardChoiceSet(selected.ToArray());
        }

        private void ApplyReward(CombatRewardOption reward)
        {
            switch (reward.Kind)
            {
                case CombatRewardKind.GlobalAttackRate:
                    combatLoadout?.ModifyAllWeaponsAttackRate(0.25f);
                    break;
                case CombatRewardKind.GlobalProjectileDamage:
                    combatLoadout?.ModifyAllWeaponsDamage(0.5f);
                    break;
                case CombatRewardKind.GlobalProjectileCount:
                    combatLoadout?.ModifyAllWeaponsProjectileCount(1);
                    break;
                case CombatRewardKind.AcquireWeapon:
                    combatLoadout?.AddWeapon(reward.WeaponDefinition);
                    break;
                case CombatRewardKind.UpgradeWeaponAttackRate:
                case CombatRewardKind.UpgradeWeaponDamage:
                case CombatRewardKind.UpgradeWeaponProjectileCount:
                    combatLoadout?.UpgradeWeapon(reward.TargetWeaponId, reward.Kind);
                    break;
                case CombatRewardKind.ActiveRadius:
                    combatLoadout?.GetActive(0)?.ModifyRadius(0.6f);
                    break;
                case CombatRewardKind.MaxHealth:
                    if (health != null)
                    {
                        health.SetMaxHealth(health.MaxHealth + 3f, true);
                    }
                    break;
                case CombatRewardKind.CorruptionPulse:
                    corruptionMeter?.AddCorruption(8f);
                    break;
            }
        }
    }
}
