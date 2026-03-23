using System.Collections.Generic;
using Dagon.Core;
using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CorruptionRuntimeEffects : MonoBehaviour
    {
        public readonly struct CorruptionOptionView
        {
            public CorruptionOptionView(string title, string description, bool isCorruptionActive = false)
            {
                Title = title;
                Description = description;
                IsCorruptionActive = isCorruptionActive;
            }

            public string Title { get; }
            public string Description { get; }
            public bool IsCorruptionActive { get; }
        }

        public readonly struct CorruptionChoiceView
        {
            public CorruptionChoiceView(int stageIndex, float thresholdValue, CorruptionOptionView[] boons, CorruptionOptionView[] drawbacks)
            {
                StageIndex = stageIndex;
                ThresholdValue = thresholdValue;
                Boons = boons;
                Drawbacks = drawbacks;
            }

            public int StageIndex { get; }
            public float ThresholdValue { get; }
            public CorruptionOptionView[] Boons { get; }
            public CorruptionOptionView[] Drawbacks { get; }
        }

        private enum EffectKind
        {
            AttackRateBonus,
            DamageBonus,
            ActiveRadiusBonus,
            MaxHealthBonus,
            HealingMultiplier,
            ContactDamageMultiplier,
            IncomingDamageMultiplier,
            CorruptionGainMultiplier,
            FodderWaveSizeMultiplier,
            SpecialistWaveSizeMultiplier,
            EliteWaveSizeMultiplier,
            SpecialistCapBonus,
            EliteCapBonus,
            BossAmbientIntervalMultiplier,
            EliteWaveEarlyUnlock,
            ReplacePrimaryActive
        }

        private readonly struct CorruptionEffect
        {
            public CorruptionEffect(EffectKind kind, float value, bool fillBonusHealth = false, ActiveAbilityDefinition activeDefinition = null)
            {
                Kind = kind;
                Value = value;
                FillBonusHealth = fillBonusHealth;
                ActiveDefinition = activeDefinition;
            }

            public EffectKind Kind { get; }
            public float Value { get; }
            public bool FillBonusHealth { get; }
            public ActiveAbilityDefinition ActiveDefinition { get; }
            public bool IsActiveReplacement => Kind == EffectKind.ReplacePrimaryActive && ActiveDefinition != null;
        }

        private readonly struct StageOptionDefinition
        {
            public StageOptionDefinition(string title, string description, CorruptionEffect effect)
            {
                Title = title;
                Description = description;
                Effect = effect;
            }

            public string Title { get; }
            public string Description { get; }
            public CorruptionEffect Effect { get; }
        }

        private readonly struct StageSelection
        {
            public StageSelection(int boonIndex, int drawbackIndex)
            {
                BoonIndex = boonIndex;
                DrawbackIndex = drawbackIndex;
            }

            public int BoonIndex { get; }
            public int DrawbackIndex { get; }
        }

        private sealed class StageAggregate
        {
            private const float BaseCorruptionGainMultiplier = 0.6f;

            public float AttackRateBonus;
            public float DamageBonus;
            public float ActiveRadiusBonus;
            public float BonusMaxHealth;
            public bool FillBonusHealth;
            public float HealingMultiplier = 1f;
            public float ContactDamageMultiplier = 1f;
            public float IncomingDamageMultiplier = 1f;
            public float CorruptionGainMultiplier = BaseCorruptionGainMultiplier;
            public float FodderWaveSizeMultiplier = 1f;
            public float SpecialistWaveSizeMultiplier = 1f;
            public float EliteWaveSizeMultiplier = 1f;
            public int SpecialistCapBonus;
            public int EliteCapBonus;
            public float BossAmbientIntervalMultiplier = 1f;
            public bool EliteWaveEarlyUnlock;
        }

        [SerializeField] private CorruptionMeter corruptionMeter;
        [SerializeField] private PlayerCombatLoadout combatLoadout;
        [SerializeField] private Health health;
        [SerializeField] private SpawnDirector spawnDirector;

        private readonly Queue<int> pendingStageChoices = new();
        private readonly Dictionary<int, StageSelection> rememberedSelections = new();
        private readonly Dictionary<int, StageOptionDefinition[]> stageBoons = new();
        private readonly Dictionary<int, StageOptionDefinition[]> stageDrawbacks = new();

        private float appliedAttackRateBonus;
        private float appliedDamageBonus;
        private float appliedActiveRadiusBonus;
        private ActiveAbilityDefinition abyssalRebirthAbility;
        private ActiveAbilityDefinition bloodwakeStepAbility;
        private ActiveAbilityDefinition riftheartAbility;

        public bool HasPendingChoice => pendingStageChoices.Count > 0;

        private void Awake()
        {
            if (corruptionMeter == null)
            {
                corruptionMeter = GetComponent<CorruptionMeter>();
            }

            if (combatLoadout == null)
            {
                combatLoadout = GetComponent<PlayerCombatLoadout>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            BuildCorruptionActiveDefinitions();
        }

        private void OnEnable()
        {
            if (corruptionMeter != null)
            {
                corruptionMeter.StageChanged += HandleStageChanged;
            }

            ReapplyActiveEffects();
        }

        private void OnDisable()
        {
            if (corruptionMeter != null)
            {
                corruptionMeter.StageChanged -= HandleStageChanged;
            }
        }

        public void Configure(SpawnDirector director)
        {
            spawnDirector = director;
            ReapplyActiveEffects();
        }

        public CorruptionChoiceView PeekPendingChoice()
        {
            if (pendingStageChoices.Count <= 0)
            {
                return default;
            }

            var stageIndex = pendingStageChoices.Peek();
            var boons = GetResolvedStageBoons(stageIndex);
            var drawbacks = GetResolvedStageDrawbacks(stageIndex);
            return new CorruptionChoiceView(
                stageIndex,
                corruptionMeter != null ? corruptionMeter.GetThresholdValue(stageIndex) : (stageIndex + 1) * 25f,
                ToOptionViews(boons),
                ToOptionViews(drawbacks));
        }

        public void ApplyPendingChoice(int boonIndex, int drawbackIndex)
        {
            if (pendingStageChoices.Count <= 0)
            {
                return;
            }

            var stageIndex = pendingStageChoices.Dequeue();
            var boons = GetResolvedStageBoons(stageIndex);
            var drawbacks = GetResolvedStageDrawbacks(stageIndex);
            var resolvedBoonIndex = Mathf.Clamp(boonIndex, 0, boons.Length - 1);
            var resolvedDrawbackIndex = Mathf.Clamp(drawbackIndex, 0, drawbacks.Length - 1);
            rememberedSelections[stageIndex] = new StageSelection(resolvedBoonIndex, resolvedDrawbackIndex);

            var chosenBoon = boons[resolvedBoonIndex].Effect;
            if (chosenBoon.IsActiveReplacement)
            {
                combatLoadout?.ReplacePrimaryActive(chosenBoon.ActiveDefinition, true);
                appliedActiveRadiusBonus = 0f;
            }

            ReapplyActiveEffects();
        }

        private void HandleStageChanged(int previousStageIndex, int currentStageIndex)
        {
            if (currentStageIndex > previousStageIndex)
            {
                for (var stageIndex = previousStageIndex + 1; stageIndex <= currentStageIndex; stageIndex++)
                {
                    if (rememberedSelections.ContainsKey(stageIndex) || pendingStageChoices.Contains(stageIndex))
                    {
                        continue;
                    }

                    GetResolvedStageBoons(stageIndex);
                    GetResolvedStageDrawbacks(stageIndex);
                    pendingStageChoices.Enqueue(stageIndex);
                }
            }

            ReapplyActiveEffects();
        }

        private void ReapplyActiveEffects()
        {
            var aggregate = BuildAggregate();

            var attackRateDelta = aggregate.AttackRateBonus - appliedAttackRateBonus;
            if (Mathf.Abs(attackRateDelta) > 0.0001f)
            {
                combatLoadout?.ModifyAllWeaponsAttackRate(attackRateDelta);
                appliedAttackRateBonus = aggregate.AttackRateBonus;
            }

            var damageDelta = aggregate.DamageBonus - appliedDamageBonus;
            if (Mathf.Abs(damageDelta) > 0.0001f)
            {
                combatLoadout?.ModifyAllWeaponsDamage(damageDelta);
                appliedDamageBonus = aggregate.DamageBonus;
            }

            var activeRadiusDelta = aggregate.ActiveRadiusBonus - appliedActiveRadiusBonus;
            if (Mathf.Abs(activeRadiusDelta) > 0.0001f)
            {
                combatLoadout?.GetPrimaryActive()?.ModifyRadius(activeRadiusDelta);
                appliedActiveRadiusBonus = aggregate.ActiveRadiusBonus;
            }

            if (health != null)
            {
                health.SetBonusMaxHealth(aggregate.BonusMaxHealth, aggregate.FillBonusHealth);
                health.SetHealingMultiplier(aggregate.HealingMultiplier);
                health.SetIncomingContactDamageMultiplier(aggregate.ContactDamageMultiplier);
                health.SetIncomingDamageMultiplier(aggregate.IncomingDamageMultiplier);
            }

            corruptionMeter?.SetCorruptionGainMultiplier(aggregate.CorruptionGainMultiplier);
            spawnDirector?.ConfigureCorruptionModifiers(
                aggregate.FodderWaveSizeMultiplier,
                aggregate.SpecialistWaveSizeMultiplier,
                aggregate.EliteWaveSizeMultiplier,
                aggregate.SpecialistCapBonus,
                aggregate.EliteCapBonus,
                aggregate.EliteWaveEarlyUnlock,
                aggregate.BossAmbientIntervalMultiplier);
        }

        private StageAggregate BuildAggregate()
        {
            var aggregate = new StageAggregate();
            var activeStageIndex = corruptionMeter != null ? corruptionMeter.CurrentStageIndex : -1;

            for (var stageIndex = 0; stageIndex <= activeStageIndex; stageIndex++)
            {
                if (!rememberedSelections.TryGetValue(stageIndex, out var selection))
                {
                    continue;
                }

                ApplyEffect(aggregate, GetResolvedStageBoons(stageIndex)[selection.BoonIndex].Effect);
                ApplyEffect(aggregate, GetResolvedStageDrawbacks(stageIndex)[selection.DrawbackIndex].Effect);
            }

            return aggregate;
        }

        private static void ApplyEffect(StageAggregate aggregate, CorruptionEffect effect)
        {
            switch (effect.Kind)
            {
                case EffectKind.AttackRateBonus:
                    aggregate.AttackRateBonus += effect.Value;
                    break;
                case EffectKind.DamageBonus:
                    aggregate.DamageBonus += effect.Value;
                    break;
                case EffectKind.ActiveRadiusBonus:
                    aggregate.ActiveRadiusBonus += effect.Value;
                    break;
                case EffectKind.MaxHealthBonus:
                    aggregate.BonusMaxHealth += effect.Value;
                    aggregate.FillBonusHealth |= effect.FillBonusHealth;
                    break;
                case EffectKind.HealingMultiplier:
                    aggregate.HealingMultiplier *= effect.Value;
                    break;
                case EffectKind.ContactDamageMultiplier:
                    aggregate.ContactDamageMultiplier *= effect.Value;
                    break;
                case EffectKind.IncomingDamageMultiplier:
                    aggregate.IncomingDamageMultiplier *= effect.Value;
                    break;
                case EffectKind.CorruptionGainMultiplier:
                    aggregate.CorruptionGainMultiplier *= effect.Value;
                    break;
                case EffectKind.FodderWaveSizeMultiplier:
                    aggregate.FodderWaveSizeMultiplier *= effect.Value;
                    break;
                case EffectKind.SpecialistWaveSizeMultiplier:
                    aggregate.SpecialistWaveSizeMultiplier *= effect.Value;
                    break;
                case EffectKind.EliteWaveSizeMultiplier:
                    aggregate.EliteWaveSizeMultiplier *= effect.Value;
                    break;
                case EffectKind.SpecialistCapBonus:
                    aggregate.SpecialistCapBonus += Mathf.RoundToInt(effect.Value);
                    break;
                case EffectKind.EliteCapBonus:
                    aggregate.EliteCapBonus += Mathf.RoundToInt(effect.Value);
                    break;
                case EffectKind.BossAmbientIntervalMultiplier:
                    aggregate.BossAmbientIntervalMultiplier *= effect.Value;
                    break;
                case EffectKind.EliteWaveEarlyUnlock:
                    aggregate.EliteWaveEarlyUnlock = effect.Value > 0.5f;
                    break;
                case EffectKind.ReplacePrimaryActive:
                    break;
            }
        }

        private static CorruptionOptionView[] ToOptionViews(StageOptionDefinition[] options)
        {
            var views = new CorruptionOptionView[options.Length];
            for (var index = 0; index < options.Length; index++)
            {
                views[index] = new CorruptionOptionView(options[index].Title, options[index].Description, options[index].Effect.IsActiveReplacement);
            }

            return views;
        }

        private StageOptionDefinition[] GetResolvedStageBoons(int stageIndex)
        {
            if (!stageBoons.TryGetValue(stageIndex, out var definitions))
            {
                definitions = BuildStageBoons(stageIndex);
                stageBoons[stageIndex] = definitions;
            }

            return definitions;
        }

        private StageOptionDefinition[] GetResolvedStageDrawbacks(int stageIndex)
        {
            if (!stageDrawbacks.TryGetValue(stageIndex, out var definitions))
            {
                definitions = BuildStageDrawbacks(stageIndex);
                stageDrawbacks[stageIndex] = definitions;
            }

            return definitions;
        }

        private StageOptionDefinition[] BuildStageBoons(int stageIndex)
        {
            var boons = stageIndex switch
            {
                0 => new[]
                {
                    new StageOptionDefinition("+15% Fire Rate", "All weapons fire faster.", new CorruptionEffect(EffectKind.AttackRateBonus, 0.15f)),
                    new StageOptionDefinition("+10% Damage", "All weapons hit harder.", new CorruptionEffect(EffectKind.DamageBonus, 0.10f)),
                    new StageOptionDefinition("+15% Radius", "Your active ability grows wider.", new CorruptionEffect(EffectKind.ActiveRadiusBonus, 0.15f))
                },
                1 => new[]
                {
                    new StageOptionDefinition("+20% Fire Rate", "All weapons fire faster.", new CorruptionEffect(EffectKind.AttackRateBonus, 0.20f)),
                    new StageOptionDefinition("+20% Damage", "All weapons hit harder.", new CorruptionEffect(EffectKind.DamageBonus, 0.20f)),
                    new StageOptionDefinition("+1 Max Heart", "Gain one filled heart.", new CorruptionEffect(EffectKind.MaxHealthBonus, 1f, fillBonusHealth: true))
                },
                2 => new[]
                {
                    new StageOptionDefinition("+25% Fire Rate", "All weapons fire faster.", new CorruptionEffect(EffectKind.AttackRateBonus, 0.25f)),
                    new StageOptionDefinition("+25% Damage", "All weapons hit harder.", new CorruptionEffect(EffectKind.DamageBonus, 0.25f)),
                    new StageOptionDefinition("+25% Radius", "Your active ability grows wider.", new CorruptionEffect(EffectKind.ActiveRadiusBonus, 0.25f))
                },
                _ => new[]
                {
                    new StageOptionDefinition("+35% Fire Rate", "All weapons fire much faster.", new CorruptionEffect(EffectKind.AttackRateBonus, 0.35f)),
                    new StageOptionDefinition("+35% Damage", "All weapons hit much harder.", new CorruptionEffect(EffectKind.DamageBonus, 0.35f)),
                    new StageOptionDefinition("+2 Max Hearts", "Gain two filled hearts.", new CorruptionEffect(EffectKind.MaxHealthBonus, 2f, fillBonusHealth: true))
                }
            };

            if (stageIndex < 1)
            {
                return boons;
            }

            var activeDefinition = DrawCorruptionActiveDefinition();
            if (activeDefinition == null)
            {
                return boons;
            }

            boons[boons.Length - 1] = new StageOptionDefinition(
                $"Corruption Active: {activeDefinition.DisplayName}",
                $"Replace {GetCurrentActiveName()} with {activeDefinition.DisplayName}. Keeps active rank.",
                new CorruptionEffect(EffectKind.ReplacePrimaryActive, 0f, false, activeDefinition));
            return boons;
        }

        private static StageOptionDefinition[] BuildStageDrawbacks(int stageIndex)
        {
            return stageIndex switch
            {
                0 => new[]
                {
                    new StageOptionDefinition("-25% Healing", "Healing restores less.", new CorruptionEffect(EffectKind.HealingMultiplier, 0.75f)),
                    new StageOptionDefinition("+20% Fodder Waves", "Fodder waves grow larger.", new CorruptionEffect(EffectKind.FodderWaveSizeMultiplier, 1.20f)),
                    new StageOptionDefinition("+15% Corruption Gain", "Pickups push corruption faster.", new CorruptionEffect(EffectKind.CorruptionGainMultiplier, 1.15f))
                },
                1 => new[]
                {
                    new StageOptionDefinition("+1 Specialist Cap", "One more specialist may stay active.", new CorruptionEffect(EffectKind.SpecialistCapBonus, 1f)),
                    new StageOptionDefinition("+20% Specialist Waves", "Specialist waves grow larger.", new CorruptionEffect(EffectKind.SpecialistWaveSizeMultiplier, 1.20f)),
                    new StageOptionDefinition("-40% Healing", "Healing restores much less.", new CorruptionEffect(EffectKind.HealingMultiplier, 0.60f))
                },
                2 => new[]
                {
                    new StageOptionDefinition("Early Elite Waves", "Elite waves unlock immediately.", new CorruptionEffect(EffectKind.EliteWaveEarlyUnlock, 1f)),
                    new StageOptionDefinition("+25% Elite Waves", "Elite waves grow larger.", new CorruptionEffect(EffectKind.EliteWaveSizeMultiplier, 1.25f)),
                    new StageOptionDefinition("+15% Contact Damage", "Body hits hurt more.", new CorruptionEffect(EffectKind.ContactDamageMultiplier, 1.15f))
                },
                _ => new[]
                {
                    new StageOptionDefinition("+1 Elite Cap", "One more elite may stay active.", new CorruptionEffect(EffectKind.EliteCapBonus, 1f)),
                    new StageOptionDefinition("Boss Pressure Up", "Boss ambient spawns arrive faster.", new CorruptionEffect(EffectKind.BossAmbientIntervalMultiplier, 1.2f)),
                    new StageOptionDefinition("+25% All Damage", "All enemy damage hurts more.", new CorruptionEffect(EffectKind.IncomingDamageMultiplier, 1.25f))
                }
            };
        }

        private string GetCurrentActiveName()
        {
            var active = combatLoadout != null ? combatLoadout.GetPrimaryActive() : null;
            return active != null ? active.DisplayName : "your active";
        }

        private ActiveAbilityDefinition DrawCorruptionActiveDefinition()
        {
            var pool = ListPool<ActiveAbilityDefinition>.Get();
            try
            {
                var currentActiveId = combatLoadout != null ? combatLoadout.GetPrimaryActive()?.AbilityId : null;
                AddEligibleActive(pool, abyssalRebirthAbility, currentActiveId);
                AddEligibleActive(pool, bloodwakeStepAbility, currentActiveId);
                AddEligibleActive(pool, riftheartAbility, currentActiveId);
                if (pool.Count <= 0)
                {
                    return null;
                }

                return pool[Random.Range(0, pool.Count)];
            }
            finally
            {
                ListPool<ActiveAbilityDefinition>.Release(pool);
            }
        }

        private static void AddEligibleActive(List<ActiveAbilityDefinition> pool, ActiveAbilityDefinition definition, string currentActiveId)
        {
            if (definition == null || definition.AbilityId == currentActiveId)
            {
                return;
            }

            pool.Add(definition);
        }

        private void BuildCorruptionActiveDefinitions()
        {
            abyssalRebirthAbility = ActiveAbilityDefinition.CreateRuntime(
                "ability.abyssal_rebirth",
                "Abyssal Rebirth",
                "Detonate a wide corruption burst and briefly become untouchable.",
                ActiveAbilityRuntimeKind.AbyssalRebirth,
                11f,
                5.8f,
                6.5f,
                durationSeconds: 0.6f);
            bloodwakeStepAbility = ActiveAbilityDefinition.CreateRuntime(
                "ability.bloodwake_step",
                "Bloodwake Step",
                "Dash through bodies and rupture the wake at both ends of the step.",
                ActiveAbilityRuntimeKind.BloodwakeStep,
                8f,
                6.6f,
                5f,
                durationSeconds: 0.2f);
            riftheartAbility = ActiveAbilityDefinition.CreateRuntime(
                "ability.riftheart",
                "Riftheart",
                "Overclock your weapons and orbit corruption shards around your body.",
                ActiveAbilityRuntimeKind.Riftheart,
                15f,
                1.9f,
                1f,
                durationSeconds: 4.5f,
                magnitude: 2.15f);
        }

        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new();

            public static List<T> Get()
            {
                return Pool.Count > 0 ? Pool.Pop() : new List<T>();
            }

            public static void Release(List<T> list)
            {
                if (list == null)
                {
                    return;
                }

                list.Clear();
                Pool.Push(list);
            }
        }
    }
}
