using System;
using System.Collections.Generic;
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
            var selected = new List<CombatRewardOption>(3);
            var usedKeys = new HashSet<string>();

            AddWeightedPathOffer(selected, usedKeys);
            AddWeightedPathOffer(selected, usedKeys);
            AddNewWeaponActiveOrGlobalOffer(selected, usedKeys);

            while (selected.Count < 3 && AddWeightedPathOffer(selected, usedKeys))
            {
            }

            while (selected.Count < 3 && AddGlobalOffer(selected, usedKeys))
            {
            }

            return new CombatRewardChoiceSet(selected.ToArray());
        }

        private void ApplyReward(CombatRewardOption reward)
        {
            switch (reward.Kind)
            {
                case CombatRewardKind.AcquireWeapon:
                    combatLoadout?.AddWeapon(reward.WeaponDefinition);
                    break;
                case CombatRewardKind.UpgradeWeaponPath:
                    if (reward.UpgradePath.HasValue)
                    {
                        combatLoadout?.UpgradeWeapon(reward.TargetWeaponId, reward.UpgradePath.Value);
                    }
                    break;
                case CombatRewardKind.UpgradeActiveAbility:
                    combatLoadout?.UpgradeActive(reward.TargetAbilityId);
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

        private bool AddWeightedPathOffer(List<CombatRewardOption> selected, HashSet<string> usedKeys)
        {
            if (combatLoadout == null)
            {
                return false;
            }

            var candidates = new List<(CombatRewardOption reward, float weight)>();
            foreach (var weapon in combatLoadout.Weapons)
            {
                AddPathCandidate(weapon, WeaponUpgradePath.PathA, candidates, usedKeys);
                AddPathCandidate(weapon, WeaponUpgradePath.PathB, candidates, usedKeys);
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            var chosen = SelectWeighted(candidates);
            selected.Add(chosen.reward);
            usedKeys.Add(BuildOfferKey(chosen.reward));
            return true;
        }

        private void AddPathCandidate(
            PlayerWeaponRuntime weapon,
            WeaponUpgradePath path,
            List<(CombatRewardOption reward, float weight)> candidates,
            HashSet<string> usedKeys)
        {
            if (weapon == null || !weapon.TryBuildPathReward(path, out var reward))
            {
                return;
            }

            var key = BuildOfferKey(reward);
            if (usedKeys.Contains(key))
            {
                return;
            }

            candidates.Add((reward, weapon.GetPathSelectionWeight(path)));
        }

        private void AddNewWeaponActiveOrGlobalOffer(List<CombatRewardOption> selected, HashSet<string> usedKeys)
        {
            var candidates = new List<(CombatRewardOption reward, float weight)>();
            if (combatLoadout != null)
            {
                foreach (var definition in combatLoadout.GetAvailableWeaponOffers())
                {
                    var reward = new CombatRewardOption(
                        CombatRewardKind.AcquireWeapon,
                        $"Claim {definition.DisplayName}",
                        definition.Description,
                        definition);
                    if (!usedKeys.Contains(BuildOfferKey(reward)))
                    {
                        candidates.Add((reward, GetWeaponOfferWeight(definition.WeaponId)));
                    }
                }

                var active = combatLoadout.GetPrimaryActive();
                if (active != null && active.TryBuildUpgradeReward(out var activeReward) && !usedKeys.Contains(BuildOfferKey(activeReward)))
                {
                    candidates.Add((activeReward, 9.5f));
                }
            }

            AddGlobalCandidates(candidates, usedKeys);
            if (candidates.Count == 0)
            {
                return;
            }

            var chosen = SelectWeighted(candidates);
            selected.Add(chosen.reward);
            usedKeys.Add(BuildOfferKey(chosen.reward));
        }

        private bool AddGlobalOffer(List<CombatRewardOption> selected, HashSet<string> usedKeys)
        {
            var candidates = new List<(CombatRewardOption reward, float weight)>();
            AddGlobalCandidates(candidates, usedKeys);
            if (candidates.Count == 0)
            {
                return false;
            }

            var chosen = SelectWeighted(candidates);
            selected.Add(chosen.reward);
            usedKeys.Add(BuildOfferKey(chosen.reward));
            return true;
        }

        private void AddGlobalCandidates(List<(CombatRewardOption reward, float weight)> candidates, HashSet<string> usedKeys)
        {
            var healthReward = new CombatRewardOption(
                CombatRewardKind.MaxHealth,
                "Salt-Hardened",
                "Increase max health.");
            if (!usedKeys.Contains(BuildOfferKey(healthReward)))
            {
                candidates.Add((healthReward, 4f));
            }

            var corruptionReward = new CombatRewardOption(
                CombatRewardKind.CorruptionPulse,
                "Tide of Dagon",
                "Gain corruption and empower the run.");
            if (!usedKeys.Contains(BuildOfferKey(corruptionReward)))
            {
                candidates.Add((corruptionReward, 4f));
            }
        }

        private static string BuildOfferKey(CombatRewardOption reward)
        {
            return $"{reward.Kind}:{reward.TargetWeaponId}:{reward.TargetAbilityId}:{reward.UpgradePath}:{reward.Title}";
        }

        private static float GetWeaponOfferWeight(string weaponId)
        {
            return weaponId switch
            {
                "weapon.anchor_chain" => 10f,
                "weapon.rot_lantern" => 9f,
                "weapon.bilge_spray" => 8f,
                "weapon.rot_beacon_bomb" => 8.5f,
                "weapon.floodline" => 8.75f,
                "weapon.tideburst" => 8.25f,
                _ => 5f
            };
        }

        private static (CombatRewardOption reward, float weight) SelectWeighted(List<(CombatRewardOption reward, float weight)> candidates)
        {
            var totalWeight = 0f;
            for (var i = 0; i < candidates.Count; i++)
            {
                totalWeight += Mathf.Max(0.01f, candidates[i].weight);
            }

            var roll = UnityEngine.Random.value * totalWeight;
            for (var i = 0; i < candidates.Count; i++)
            {
                roll -= Mathf.Max(0.01f, candidates[i].weight);
                if (roll <= 0f)
                {
                    return candidates[i];
                }
            }

            return candidates[candidates.Count - 1];
        }
    }
}
