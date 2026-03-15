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

        private readonly Queue<UpgradeChoiceSet> queuedChoices = new();
        private HarpoonLauncher harpoonLauncher;
        private Health health;
        private CorruptionMeter corruptionMeter;
        private BrineSurgeAbility brineSurgeAbility;

        public int Level { get; private set; }
        public int CurrentXp { get; private set; }
        public int RequiredXp { get; private set; }
        public bool HasPendingChoice => queuedChoices.Count > 0;

        public event Action Changed;

        private void Awake()
        {
            harpoonLauncher = GetComponent<HarpoonLauncher>();
            health = GetComponent<Health>();
            corruptionMeter = GetComponent<CorruptionMeter>();
            brineSurgeAbility = GetComponent<BrineSurgeAbility>();

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

        public UpgradeChoiceSet PeekChoices()
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

            ApplyUpgrade(choices.Options[index]);
            Changed?.Invoke();
        }

        private int GetRequiredXpForLevel(int level)
        {
            return baseXpRequirement + ((level - 1) * xpRequirementGrowth);
        }

        private UpgradeChoiceSet BuildChoiceSet()
        {
            return new UpgradeChoiceSet(
                new[]
                {
                    UpgradeChoice.AttackRate,
                    UpgradeChoice.ProjectileDamage,
                    UpgradeChoice.ProjectileCount
                });
        }

        private void ApplyUpgrade(UpgradeChoice choice)
        {
            switch (choice)
            {
                case UpgradeChoice.AttackRate:
                    harpoonLauncher?.ModifyAttacksPerSecond(0.25f);
                    break;
                case UpgradeChoice.ProjectileDamage:
                    harpoonLauncher?.ModifyProjectileDamage(0.5f);
                    break;
                case UpgradeChoice.ProjectileCount:
                    harpoonLauncher?.ModifyProjectileCount(1);
                    break;
                case UpgradeChoice.BrineRadius:
                    brineSurgeAbility?.ModifyRadius(0.6f);
                    break;
                case UpgradeChoice.MaxHealth:
                    if (health != null)
                    {
                        health.SetMaxHealth(health.MaxHealth + 3f, true);
                    }
                    break;
                case UpgradeChoice.CorruptionPulse:
                    corruptionMeter?.AddCorruption(8f);
                    break;
            }
        }
    }

    public readonly struct UpgradeChoiceSet
    {
        public UpgradeChoiceSet(UpgradeChoice[] options)
        {
            Options = options;
        }

        public UpgradeChoice[] Options { get; }
    }

    public enum UpgradeChoice
    {
        AttackRate,
        ProjectileDamage,
        ProjectileCount,
        BrineRadius,
        MaxHealth,
        CorruptionPulse
    }
}
