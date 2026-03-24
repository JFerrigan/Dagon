using System.Collections.Generic;
using Dagon.Core;
using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CorruptionRuntimeEffects : MonoBehaviour
    {
        private enum BoonBand
        {
            Early,
            Mid,
            Late
        }

        private enum DrawbackBand
        {
            Early,
            Mid,
            Late
        }

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
            public CorruptionChoiceView(int stageIndex, float thresholdValue, CorruptionOptionView[] boons, CorruptionOptionView[] drawbacks, bool requiresBoonSelection)
            {
                StageIndex = stageIndex;
                ThresholdValue = thresholdValue;
                Boons = boons;
                Drawbacks = drawbacks;
                RequiresBoonSelection = requiresBoonSelection;
            }

            public int StageIndex { get; }
            public float ThresholdValue { get; }
            public CorruptionOptionView[] Boons { get; }
            public CorruptionOptionView[] Drawbacks { get; }
            public bool RequiresBoonSelection { get; }
        }

        private enum EffectKind
        {
            AttackRateBonus,
            HealingMultiplier,
            IncomingDamageMultiplier,
            EliteWaveSizeMultiplier,
            BossAmbientIntervalMultiplier,
            EliteWaveEarlyUnlock,
            ReplacePrimaryActive,
            AmbientSpawnIntervalMultiplier,
            EnemyHealthMultiplier,
            EnemyMoveSpeedMultiplier,
            ActiveCooldownMultiplier,
            DisableWorldHealing,
            BossCorruptionChanceMultiplier,
            CorruptedBossHealthMultiplier,
            AmbientBossLaneEnable,
            ResetProgression,
            BloodInTheWake,
            CarrionPull,
            ExperiencePickupValueMultiplier,
            BrineEngine,
            CrashingSurge,
            UpgradeAllWeaponsOnce,
            AbyssalPulse,
            DevourTheDeep
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
            public bool IsResetProgression => Kind == EffectKind.ResetProgression;
            public bool UpgradesAllWeapons => Kind == EffectKind.UpgradeAllWeaponsOnce;
        }

        private readonly struct StageOptionDefinition
        {
            public StageOptionDefinition(string title, string description, params CorruptionEffect[] effects)
            {
                Title = title;
                Description = description;
                Effects = effects ?? System.Array.Empty<CorruptionEffect>();
            }

            public string Title { get; }
            public string Description { get; }
            public CorruptionEffect[] Effects { get; }

            public bool IsActiveReplacement
            {
                get
                {
                    for (var i = 0; i < Effects.Length; i++)
                    {
                        if (Effects[i].IsActiveReplacement)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            public bool TriggersProgressionReset
            {
                get
                {
                    for (var i = 0; i < Effects.Length; i++)
                    {
                        if (Effects[i].IsResetProgression)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        }

        private readonly struct WeightedBoonDefinition
        {
            public WeightedBoonDefinition(BoonBand band, string title, string description, params CorruptionEffect[] effects)
            {
                Band = band;
                Option = new StageOptionDefinition(title, description, effects);
            }

            public BoonBand Band { get; }
            public StageOptionDefinition Option { get; }
        }

        private readonly struct WeightedDrawbackDefinition
        {
            public WeightedDrawbackDefinition(DrawbackBand band, string title, string description, params CorruptionEffect[] effects)
            {
                Band = band;
                Option = new StageOptionDefinition(title, description, effects);
            }

            public DrawbackBand Band { get; }
            public StageOptionDefinition Option { get; }
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
            public float HealingMultiplier = 1f;
            public float IncomingDamageMultiplier = 1f;
            public float EliteWaveSizeMultiplier = 1f;
            public float BossAmbientIntervalMultiplier = 1f;
            public bool EliteWaveEarlyUnlock;
            public float CorruptionGainMultiplier = BaseCorruptionGainMultiplier;
            public float AmbientSpawnIntervalMultiplier = 1f;
            public float EnemyHealthMultiplier = 1f;
            public float EnemyMoveSpeedMultiplier = 1f;
            public float ActiveCooldownMultiplier = 1f;
            public bool WorldHealingDisabled;
            public float BossCorruptionChanceMultiplier = 1f;
            public float CorruptedBossHealthMultiplier = 1f;
            public bool AmbientBossLaneEnabled;
            public bool BloodInTheWake;
            public float PickupAttractRadiusMultiplier = 1f;
            public float ExperiencePickupValueMultiplier = 1f;
            public bool BrineEngine;
            public bool CrashingSurge;
            public bool AbyssalPulse;
            public bool DevourTheDeep;
        }

        private const int NormalCorruptionStageCount = 9;
        private const int CatastropheStageIndex = 9;

        private const float BloodInTheWakeDuration = 3.5f;
        private const float BloodInTheWakeBonusPerStack = 0.08f;
        private const int BloodInTheWakeMaxStacks = 3;
        private const float BrineEngineBuildTime = 1.4f;
        private const float BrineEngineMaxBonus = 0.18f;
        private const float CrashingSurgeDuration = 2.5f;
        private const float CrashingSurgeBonus = 0.22f;
        private const int AbyssalPulseKillsPerTrigger = 10;
        private const float AbyssalPulseRadius = 4.5f;
        private const float AbyssalPulseDamage = 6f;
        private const float DevourTheDeepCorruptionReduction = 3f;

        [SerializeField] private CorruptionMeter corruptionMeter;
        [SerializeField] private PlayerCombatLoadout combatLoadout;
        [SerializeField] private Health health;
        [SerializeField] private SpawnDirector spawnDirector;
        [SerializeField] private ExperienceController experienceController;
        [SerializeField] private RunStateManager runStateManager;
        [SerializeField] private PlayerMover playerMover;
        [SerializeField] private LayerMask enemyMask = ~0;

        private readonly Queue<int> pendingStageChoices = new();
        private readonly Dictionary<int, StageSelection> rememberedSelections = new();
        private readonly Dictionary<int, StageOptionDefinition[]> stageBoons = new();
        private readonly Dictionary<int, StageOptionDefinition[]> stageDrawbacks = new();

        private float appliedAttackRateBonus;
        private float appliedTransientAttackRateBonus;
        private bool blocksWorldHealing;
        private float pickupAttractRadiusMultiplier = 1f;
        private float experiencePickupValueMultiplier = 1f;
        private bool bloodInTheWakeEnabled;
        private bool brineEngineEnabled;
        private bool crashingSurgeEnabled;
        private bool abyssalPulseEnabled;
        private bool devourTheDeepEnabled;
        private int bloodInTheWakeStacks;
        private float bloodInTheWakeTimer;
        private float brineEngineCharge;
        private float crashingSurgeTimer;
        private int abyssalPulseKillCounter;
        private ActiveAbilityDefinition abyssalRebirthAbility;
        private ActiveAbilityDefinition bloodwakeStepAbility;
        private ActiveAbilityDefinition riftheartAbility;
        private ActiveAbilityRuntime subscribedActive;
        private readonly HashSet<GameObject> resolvedTargets = new();

        public bool HasPendingChoice => pendingStageChoices.Count > 0;
        public bool BlocksWorldHealing => blocksWorldHealing;
        public float PickupAttractRadiusMultiplier => pickupAttractRadiusMultiplier;
        public float ExperiencePickupValueMultiplier => experiencePickupValueMultiplier;

        private void Awake()
        {
            corruptionMeter ??= GetComponent<CorruptionMeter>();
            combatLoadout ??= GetComponent<PlayerCombatLoadout>();
            health ??= GetComponent<Health>();
            experienceController ??= GetComponent<ExperienceController>();
            playerMover ??= GetComponent<PlayerMover>();
            if (runStateManager == null)
            {
                runStateManager = FindFirstObjectByType<RunStateManager>();
            }

            BuildCorruptionActiveDefinitions();
        }

        private void OnEnable()
        {
            if (corruptionMeter != null)
            {
                corruptionMeter.StageChanged += HandleStageChanged;
            }

            if (playerMover != null)
            {
                playerMover.DashStarted += HandleDashStarted;
            }

            RefreshActiveSubscription();
            ReapplyActiveEffects();
        }

        private void OnDisable()
        {
            if (corruptionMeter != null)
            {
                corruptionMeter.StageChanged -= HandleStageChanged;
            }

            if (playerMover != null)
            {
                playerMover.DashStarted -= HandleDashStarted;
            }

            UnsubscribeActive();
        }

        private void Update()
        {
            RefreshActiveSubscription();
            TickTransientPassives(Time.deltaTime);
        }

        public void Configure(SpawnDirector director)
        {
            spawnDirector = director;
            if (runStateManager == null)
            {
                runStateManager = FindFirstObjectByType<RunStateManager>();
            }

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
                ToOptionViews(drawbacks),
                boons.Length > 0);
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
            var resolvedBoonIndex = boons.Length > 0 ? Mathf.Clamp(boonIndex, 0, boons.Length - 1) : -1;
            var resolvedDrawbackIndex = stageIndex >= CatastropheStageIndex
                ? Mathf.Clamp(drawbackIndex, 0, drawbacks.Length - 1)
                : (drawbacks.Length > 0 ? 0 : -1);
            rememberedSelections[stageIndex] = new StageSelection(resolvedBoonIndex, resolvedDrawbackIndex);

            if (resolvedBoonIndex >= 0)
            {
                ApplySelectionSideEffects(boons[resolvedBoonIndex]);
            }

            if (resolvedDrawbackIndex >= 0 && resolvedDrawbackIndex < drawbacks.Length)
            {
                ApplySelectionSideEffects(drawbacks[resolvedDrawbackIndex]);
            }
            ReapplyActiveEffects();
        }

        public void NotifyEnemyKilled(GameObject enemyRoot, GameObject source)
        {
            if (!WasPlayerKill(source))
            {
                return;
            }

            if (bloodInTheWakeEnabled)
            {
                bloodInTheWakeStacks = Mathf.Min(BloodInTheWakeMaxStacks, bloodInTheWakeStacks + 1);
                bloodInTheWakeTimer = BloodInTheWakeDuration;
            }

            if (abyssalPulseEnabled)
            {
                abyssalPulseKillCounter += 1;
                while (abyssalPulseKillCounter >= AbyssalPulseKillsPerTrigger)
                {
                    abyssalPulseKillCounter -= AbyssalPulseKillsPerTrigger;
                    TriggerAbyssalPulse();
                }
            }

            if (devourTheDeepEnabled && enemyRoot != null && enemyRoot.GetComponent<CorruptedVariantMarker>() != null)
            {
                corruptionMeter?.ReduceCorruption(DevourTheDeepCorruptionReduction);
            }
        }

        private void HandleStageChanged(int previousStageIndex, int currentStageIndex)
        {
            if (currentStageIndex > previousStageIndex)
            {
                for (var stageIndex = previousStageIndex + 1; stageIndex <= currentStageIndex; stageIndex++)
                {
                    if (rememberedSelections.ContainsKey(stageIndex))
                    {
                        continue;
                    }

                    if (!pendingStageChoices.Contains(stageIndex))
                    {
                        GetResolvedStageBoons(stageIndex);
                        GetResolvedStageDrawbacks(stageIndex);
                        pendingStageChoices.Enqueue(stageIndex);
                    }
                }
            }

            ReapplyActiveEffects();
        }

        private void ApplySelectionSideEffects(StageOptionDefinition option)
        {
            for (var i = 0; i < option.Effects.Length; i++)
            {
                var effect = option.Effects[i];
                if (effect.IsActiveReplacement)
                {
                    combatLoadout?.ReplacePrimaryActive(effect.ActiveDefinition, true);
                    RefreshActiveSubscription();
                }
                else if (effect.IsResetProgression)
                {
                    PerformCastDown();
                }
                else if (effect.UpgradesAllWeapons)
                {
                    UpgradeAllWeaponsOnce();
                }
            }
        }

        private void PerformCastDown()
        {
            experienceController?.ResetToStartingProgression();
            combatLoadout?.ResetToStartingLoadout();
            RefreshActiveSubscription();
            appliedAttackRateBonus = 0f;
            appliedTransientAttackRateBonus = 0f;
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

            combatLoadout?.SetAllActiveCooldownMultiplier(aggregate.ActiveCooldownMultiplier);

            if (health != null)
            {
                health.SetHealingMultiplier(aggregate.HealingMultiplier);
                health.SetIncomingDamageMultiplier(aggregate.IncomingDamageMultiplier);
            }

            pickupAttractRadiusMultiplier = aggregate.PickupAttractRadiusMultiplier;
            experiencePickupValueMultiplier = aggregate.ExperiencePickupValueMultiplier;
            bloodInTheWakeEnabled = aggregate.BloodInTheWake;
            brineEngineEnabled = aggregate.BrineEngine;
            crashingSurgeEnabled = aggregate.CrashingSurge;
            abyssalPulseEnabled = aggregate.AbyssalPulse;
            devourTheDeepEnabled = aggregate.DevourTheDeep;
            blocksWorldHealing = aggregate.WorldHealingDisabled;

            if (!bloodInTheWakeEnabled)
            {
                bloodInTheWakeStacks = 0;
                bloodInTheWakeTimer = 0f;
            }

            if (!crashingSurgeEnabled)
            {
                crashingSurgeTimer = 0f;
            }

            if (!abyssalPulseEnabled)
            {
                abyssalPulseKillCounter = 0;
            }

            corruptionMeter?.SetCorruptionGainMultiplier(aggregate.CorruptionGainMultiplier);
            spawnDirector?.ConfigureCorruptionModifiers(
                1f,
                1f,
                aggregate.EliteWaveSizeMultiplier,
                0,
                0,
                aggregate.EliteWaveEarlyUnlock,
                aggregate.BossAmbientIntervalMultiplier,
                aggregate.AmbientSpawnIntervalMultiplier,
                aggregate.EnemyHealthMultiplier,
                aggregate.EnemyMoveSpeedMultiplier);
            runStateManager?.ConfigureCorruptionBossModifiers(
                aggregate.BossCorruptionChanceMultiplier,
                aggregate.CorruptedBossHealthMultiplier,
                aggregate.AmbientBossLaneEnabled,
                1f,
                1);
        }

        private StageAggregate BuildAggregate()
        {
            var aggregate = new StageAggregate();
            var activeStageIndex = corruptionMeter != null ? Mathf.Min(NormalCorruptionStageCount - 1, corruptionMeter.CurrentStageIndex) : -1;

            for (var stageIndex = 0; stageIndex <= activeStageIndex; stageIndex++)
            {
                if (!rememberedSelections.TryGetValue(stageIndex, out var selection))
                {
                    continue;
                }

                var boons = GetResolvedStageBoons(stageIndex);
                if (selection.BoonIndex >= 0 && selection.BoonIndex < boons.Length)
                {
                    ApplyEffects(aggregate, boons[selection.BoonIndex].Effects);
                }

                var drawbacks = GetResolvedStageDrawbacks(stageIndex);
                if (selection.DrawbackIndex >= 0 && selection.DrawbackIndex < drawbacks.Length)
                {
                    ApplyEffects(aggregate, drawbacks[selection.DrawbackIndex].Effects);
                }
            }

            if (corruptionMeter != null && corruptionMeter.CurrentStageIndex >= CatastropheStageIndex && rememberedSelections.TryGetValue(CatastropheStageIndex, out var catastrophicSelection))
            {
                var stageFive = GetResolvedStageDrawbacks(CatastropheStageIndex);
                if (catastrophicSelection.DrawbackIndex >= 0 && catastrophicSelection.DrawbackIndex < stageFive.Length)
                {
                    ApplyEffects(aggregate, stageFive[catastrophicSelection.DrawbackIndex].Effects);
                }
            }

            return aggregate;
        }

        private void TickTransientPassives(float deltaTime)
        {
            if (deltaTime <= 0f || combatLoadout == null)
            {
                return;
            }

            if (bloodInTheWakeTimer > 0f)
            {
                bloodInTheWakeTimer = Mathf.Max(0f, bloodInTheWakeTimer - deltaTime);
                if (bloodInTheWakeTimer <= 0f)
                {
                    bloodInTheWakeStacks = 0;
                }
            }

            if (crashingSurgeTimer > 0f)
            {
                crashingSurgeTimer = Mathf.Max(0f, crashingSurgeTimer - deltaTime);
            }

            if (brineEngineEnabled && playerMover != null)
            {
                var moving = playerMover.MoveDirection.sqrMagnitude > 0.05f;
                var rate = moving ? 1f : -1.5f;
                brineEngineCharge = Mathf.Clamp(brineEngineCharge + (deltaTime * rate), 0f, BrineEngineBuildTime);
            }
            else
            {
                brineEngineCharge = 0f;
            }

            var transientAttackRateBonus = 0f;
            transientAttackRateBonus += bloodInTheWakeStacks * BloodInTheWakeBonusPerStack;
            transientAttackRateBonus += (brineEngineCharge / BrineEngineBuildTime) * BrineEngineMaxBonus;
            if (crashingSurgeTimer > 0f)
            {
                transientAttackRateBonus += CrashingSurgeBonus;
            }

            var delta = transientAttackRateBonus - appliedTransientAttackRateBonus;
            if (Mathf.Abs(delta) > 0.0001f)
            {
                combatLoadout.ModifyAllWeaponsAttackRate(delta);
                appliedTransientAttackRateBonus = transientAttackRateBonus;
            }
        }

        private static void ApplyEffects(StageAggregate aggregate, CorruptionEffect[] effects)
        {
            if (effects == null)
            {
                return;
            }

            for (var i = 0; i < effects.Length; i++)
            {
                ApplyEffect(aggregate, effects[i]);
            }
        }

        private static void ApplyEffect(StageAggregate aggregate, CorruptionEffect effect)
        {
            switch (effect.Kind)
            {
                case EffectKind.AttackRateBonus:
                    aggregate.AttackRateBonus += effect.Value;
                    break;
                case EffectKind.HealingMultiplier:
                    aggregate.HealingMultiplier *= effect.Value;
                    break;
                case EffectKind.IncomingDamageMultiplier:
                    aggregate.IncomingDamageMultiplier *= effect.Value;
                    break;
                case EffectKind.EliteWaveSizeMultiplier:
                    aggregate.EliteWaveSizeMultiplier *= effect.Value;
                    break;
                case EffectKind.BossAmbientIntervalMultiplier:
                    aggregate.BossAmbientIntervalMultiplier *= effect.Value;
                    break;
                case EffectKind.EliteWaveEarlyUnlock:
                    aggregate.EliteWaveEarlyUnlock = effect.Value > 0.5f;
                    break;
                case EffectKind.AmbientSpawnIntervalMultiplier:
                    aggregate.AmbientSpawnIntervalMultiplier *= effect.Value;
                    break;
                case EffectKind.EnemyHealthMultiplier:
                    aggregate.EnemyHealthMultiplier *= effect.Value;
                    break;
                case EffectKind.EnemyMoveSpeedMultiplier:
                    aggregate.EnemyMoveSpeedMultiplier *= effect.Value;
                    break;
                case EffectKind.ActiveCooldownMultiplier:
                    aggregate.ActiveCooldownMultiplier *= effect.Value;
                    break;
                case EffectKind.DisableWorldHealing:
                    aggregate.WorldHealingDisabled = effect.Value > 0.5f;
                    break;
                case EffectKind.BossCorruptionChanceMultiplier:
                    aggregate.BossCorruptionChanceMultiplier *= effect.Value;
                    break;
                case EffectKind.CorruptedBossHealthMultiplier:
                    aggregate.CorruptedBossHealthMultiplier *= effect.Value;
                    break;
                case EffectKind.AmbientBossLaneEnable:
                    aggregate.AmbientBossLaneEnabled |= effect.Value > 0.5f;
                    break;
                case EffectKind.BloodInTheWake:
                    aggregate.BloodInTheWake = effect.Value > 0.5f;
                    break;
                case EffectKind.CarrionPull:
                    aggregate.PickupAttractRadiusMultiplier *= effect.Value;
                    break;
                case EffectKind.ExperiencePickupValueMultiplier:
                    aggregate.ExperiencePickupValueMultiplier *= effect.Value;
                    break;
                case EffectKind.BrineEngine:
                    aggregate.BrineEngine = effect.Value > 0.5f;
                    break;
                case EffectKind.CrashingSurge:
                    aggregate.CrashingSurge = effect.Value > 0.5f;
                    break;
                case EffectKind.AbyssalPulse:
                    aggregate.AbyssalPulse = effect.Value > 0.5f;
                    break;
                case EffectKind.DevourTheDeep:
                    aggregate.DevourTheDeep = effect.Value > 0.5f;
                    break;
                case EffectKind.ReplacePrimaryActive:
                case EffectKind.ResetProgression:
                case EffectKind.UpgradeAllWeaponsOnce:
                    break;
            }
        }

        private static CorruptionOptionView[] ToOptionViews(StageOptionDefinition[] options)
        {
            var views = new CorruptionOptionView[options.Length];
            for (var index = 0; index < options.Length; index++)
            {
                views[index] = new CorruptionOptionView(options[index].Title, options[index].Description, options[index].IsActiveReplacement);
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
            if (stageIndex >= NormalCorruptionStageCount)
            {
                return System.Array.Empty<StageOptionDefinition>();
            }

            var candidates = ListPool<WeightedBoonDefinition>.Get();
            var selected = ListPool<StageOptionDefinition>.Get();
            try
            {
                BuildWeightedBoonCatalog(candidates);
                if (candidates.Count == 0)
                {
                    return System.Array.Empty<StageOptionDefinition>();
                }

                while (selected.Count < 3 && candidates.Count > 0)
                {
                    var band = RollBandForStage(stageIndex, candidates);
                    var matching = ListPool<int>.Get();
                    try
                    {
                        for (var i = 0; i < candidates.Count; i++)
                        {
                            if (candidates[i].Band == band)
                            {
                                matching.Add(i);
                            }
                        }

                        if (matching.Count <= 0)
                        {
                            candidates.RemoveAt(Random.Range(0, candidates.Count));
                            continue;
                        }

                        var chosenCandidateIndex = matching[Random.Range(0, matching.Count)];
                        selected.Add(candidates[chosenCandidateIndex].Option);
                        candidates.RemoveAt(chosenCandidateIndex);
                    }
                    finally
                    {
                        ListPool<int>.Release(matching);
                    }
                }

                return selected.ToArray();
            }
            finally
            {
                ListPool<WeightedBoonDefinition>.Release(candidates);
                ListPool<StageOptionDefinition>.Release(selected);
            }
        }

        private void BuildWeightedBoonCatalog(List<WeightedBoonDefinition> catalog)
        {
            AddWeightedBoon(catalog, BoonBand.Early, "Blood in the Wake", "Kills build short stacks of fire rate.", new CorruptionEffect(EffectKind.BloodInTheWake, 1f));
            AddWeightedBoon(catalog, BoonBand.Early, "Carrion Pull", "Nearby pickups pull in from much farther away.", new CorruptionEffect(EffectKind.CarrionPull, 2f));
            AddWeightedBoon(catalog, BoonBand.Early, "Greedy Undertow", "XP pickups are worth more.", new CorruptionEffect(EffectKind.ExperiencePickupValueMultiplier, 1.35f));

            AddWeightedBoon(catalog, BoonBand.Mid, "Brine Engine", "Keep moving to build weapon tempo.", new CorruptionEffect(EffectKind.BrineEngine, 1f));
            AddWeightedBoon(catalog, BoonBand.Mid, "Crashing Surge", "Dashing or using your active grants a short fire-rate spike.", new CorruptionEffect(EffectKind.CrashingSurge, 1f));

            var activeDefinition = DrawCorruptionActiveDefinition();
            if (activeDefinition != null)
            {
                AddWeightedBoon(catalog,
                    BoonBand.Mid,
                    $"Corruption Active: {activeDefinition.DisplayName}",
                    $"Replace {GetCurrentActiveName()} with {activeDefinition.DisplayName}. Keeps active rank.",
                    new CorruptionEffect(EffectKind.ReplacePrimaryActive, 0f, false, activeDefinition));
            }

            AddWeightedBoon(catalog, BoonBand.Mid, "Tide Ascendant", "Upgrade every owned weapon once.", new CorruptionEffect(EffectKind.UpgradeAllWeaponsOnce, 1f));

            AddWeightedBoon(catalog, BoonBand.Late, "Abyssal Pulse", "Every few kills unleash a corruption pulse.", new CorruptionEffect(EffectKind.AbyssalPulse, 1f));
            AddWeightedBoon(catalog, BoonBand.Late, "Devour the Deep", "Killing corrupted enemies reduces corruption.", new CorruptionEffect(EffectKind.DevourTheDeep, 1f));
            AddWeightedBoon(catalog, BoonBand.Late, "Quickening Rot", "All weapons fire faster.", new CorruptionEffect(EffectKind.AttackRateBonus, 0.25f));
        }

        private void AddWeightedBoon(List<WeightedBoonDefinition> catalog, BoonBand band, string title, string description, params CorruptionEffect[] effects)
        {
            if (HasSelectedBoonTitle(title))
            {
                return;
            }

            catalog.Add(new WeightedBoonDefinition(band, title, description, effects));
        }

        private bool HasSelectedBoonTitle(string title)
        {
            foreach (var pair in rememberedSelections)
            {
                var stageBoonsForSelection = GetResolvedStageBoons(pair.Key);
                var boonIndex = pair.Value.BoonIndex;
                if (boonIndex < 0 || boonIndex >= stageBoonsForSelection.Length)
                {
                    continue;
                }

                if (stageBoonsForSelection[boonIndex].Title == title)
                {
                    return true;
                }
            }

            return false;
        }

        private static BoonBand RollBandForStage(int stageIndex, List<WeightedBoonDefinition> candidates)
        {
            var weights = GetStageBandWeights(stageIndex);

            var earlyAvailable = false;
            var midAvailable = false;
            var lateAvailable = false;
            for (var i = 0; i < candidates.Count; i++)
            {
                switch (candidates[i].Band)
                {
                    case BoonBand.Early:
                        earlyAvailable = true;
                        break;
                    case BoonBand.Mid:
                        midAvailable = true;
                        break;
                    case BoonBand.Late:
                        lateAvailable = true;
                        break;
                }
            }

            var total = 0f;
            if (earlyAvailable)
            {
                total += weights[0];
            }

            if (midAvailable)
            {
                total += weights[1];
            }

            if (lateAvailable)
            {
                total += weights[2];
            }

            if (total <= 0f)
            {
                return BoonBand.Early;
            }

            var roll = Random.value * total;
            if (earlyAvailable)
            {
                if (roll < weights[0])
                {
                    return BoonBand.Early;
                }

                roll -= weights[0];
            }

            if (midAvailable)
            {
                if (roll < weights[1])
                {
                    return BoonBand.Mid;
                }

                roll -= weights[1];
            }

            return lateAvailable ? BoonBand.Late : (midAvailable ? BoonBand.Mid : BoonBand.Early);
        }

        private static float[] GetStageBandWeights(int stageIndex)
        {
            return stageIndex switch
            {
                0 => new[] { 0.75f, 0.20f, 0.05f },
                1 => new[] { 0.60f, 0.30f, 0.10f },
                2 => new[] { 0.45f, 0.40f, 0.15f },
                3 => new[] { 0.30f, 0.50f, 0.20f },
                4 => new[] { 0.20f, 0.55f, 0.25f },
                5 => new[] { 0.15f, 0.50f, 0.35f },
                6 => new[] { 0.10f, 0.45f, 0.45f },
                7 => new[] { 0.05f, 0.35f, 0.60f },
                _ => new[] { 0.05f, 0.20f, 0.75f }
            };
        }

        private StageOptionDefinition[] BuildStageDrawbacks(int stageIndex)
        {
            if (stageIndex >= CatastropheStageIndex)
            {
                return new[]
                {
                    new StageOptionDefinition("No Healing", "Health pickups and fountains no longer help you.", new CorruptionEffect(EffectKind.DisableWorldHealing, 1f)),
                    new StageOptionDefinition("Bosses Become Common", "Ambient bosses start appearing like elite threats.", new CorruptionEffect(EffectKind.AmbientBossLaneEnable, 1f)),
                    new StageOptionDefinition("Cast Down", "Reset to level 1 with your starting weapons and abilities.", new CorruptionEffect(EffectKind.ResetProgression, 1f))
                };
            }

            var catalog = ListPool<WeightedDrawbackDefinition>.Get();
            try
            {
                BuildWeightedDrawbackCatalog(catalog);
                if (catalog.Count <= 0)
                {
                    return System.Array.Empty<StageOptionDefinition>();
                }

                var band = RollDrawbackBandForStage(stageIndex, catalog);
                var matches = ListPool<int>.Get();
                try
                {
                    for (var i = 0; i < catalog.Count; i++)
                    {
                        if (catalog[i].Band == band)
                        {
                            matches.Add(i);
                        }
                    }

                    if (matches.Count <= 0)
                    {
                        return new[] { catalog[Random.Range(0, catalog.Count)].Option };
                    }

                    var chosenIndex = matches[Random.Range(0, matches.Count)];
                    return new[] { catalog[chosenIndex].Option };
                }
                finally
                {
                    ListPool<int>.Release(matches);
                }
            }
            finally
            {
                ListPool<WeightedDrawbackDefinition>.Release(catalog);
            }
        }

        private static void BuildWeightedDrawbackCatalog(List<WeightedDrawbackDefinition> catalog)
        {
            catalog.Add(new WeightedDrawbackDefinition(DrawbackBand.Early, "Blighted Swarm", "Ambient swarms arrive faster.", new CorruptionEffect(EffectKind.AmbientSpawnIntervalMultiplier, 0.82f)));
            catalog.Add(new WeightedDrawbackDefinition(DrawbackBand.Early, "Tainted Swiftness", "Enemies move faster.", new CorruptionEffect(EffectKind.EnemyMoveSpeedMultiplier, 1.14f)));
            catalog.Add(new WeightedDrawbackDefinition(DrawbackBand.Early, "Ravenous Wounds", "Healing restores far less.", new CorruptionEffect(EffectKind.HealingMultiplier, 0.60f)));

            catalog.Add(new WeightedDrawbackDefinition(DrawbackBand.Mid, "Fleshwarp I", "Enemy health swells further with corruption.", new CorruptionEffect(EffectKind.EnemyHealthMultiplier, 1.20f)));
            catalog.Add(new WeightedDrawbackDefinition(DrawbackBand.Mid, "Drowned Hands", "Active abilities recover more slowly.", new CorruptionEffect(EffectKind.ActiveCooldownMultiplier, 1.45f)));
            catalog.Add(new WeightedDrawbackDefinition(DrawbackBand.Mid, "Tyrant Tide", "Boss ambient pressure worsens.", new CorruptionEffect(EffectKind.BossAmbientIntervalMultiplier, 0.78f)));

            catalog.Add(new WeightedDrawbackDefinition(DrawbackBand.Late, "The Deep Opens", "Elite waves unlock immediately.", new CorruptionEffect(EffectKind.EliteWaveEarlyUnlock, 1f)));
            catalog.Add(new WeightedDrawbackDefinition(DrawbackBand.Late, "Fleshwarp II", "Enemy health swells again.", new CorruptionEffect(EffectKind.EnemyHealthMultiplier, 1.30f)));
            catalog.Add(new WeightedDrawbackDefinition(DrawbackBand.Late, "Warhost Rising", "Elite waves grow larger.", new CorruptionEffect(EffectKind.EliteWaveSizeMultiplier, 1.25f)));
            catalog.Add(new WeightedDrawbackDefinition(
                DrawbackBand.Late,
                "Black Omen",
                "Corrupted bosses become far more likely and tougher.",
                new CorruptionEffect(EffectKind.BossCorruptionChanceMultiplier, 1.85f),
                new CorruptionEffect(EffectKind.CorruptedBossHealthMultiplier, 1.35f)));
            catalog.Add(new WeightedDrawbackDefinition(DrawbackBand.Late, "Mortal Ruin", "All enemy damage is doubled.", new CorruptionEffect(EffectKind.IncomingDamageMultiplier, 2f)));
        }

        private static DrawbackBand RollDrawbackBandForStage(int stageIndex, List<WeightedDrawbackDefinition> candidates)
        {
            var weights = GetStageBandWeights(stageIndex);
            var earlyAvailable = false;
            var midAvailable = false;
            var lateAvailable = false;
            for (var i = 0; i < candidates.Count; i++)
            {
                switch (candidates[i].Band)
                {
                    case DrawbackBand.Early:
                        earlyAvailable = true;
                        break;
                    case DrawbackBand.Mid:
                        midAvailable = true;
                        break;
                    case DrawbackBand.Late:
                        lateAvailable = true;
                        break;
                }
            }

            var total = 0f;
            if (earlyAvailable)
            {
                total += weights[0];
            }

            if (midAvailable)
            {
                total += weights[1];
            }

            if (lateAvailable)
            {
                total += weights[2];
            }

            if (total <= 0f)
            {
                return DrawbackBand.Early;
            }

            var roll = Random.value * total;
            if (earlyAvailable)
            {
                if (roll < weights[0])
                {
                    return DrawbackBand.Early;
                }

                roll -= weights[0];
            }

            if (midAvailable)
            {
                if (roll < weights[1])
                {
                    return DrawbackBand.Mid;
                }

                roll -= weights[1];
            }

            return lateAvailable ? DrawbackBand.Late : (midAvailable ? DrawbackBand.Mid : DrawbackBand.Early);
        }

        private void UpgradeAllWeaponsOnce()
        {
            if (combatLoadout == null)
            {
                return;
            }

            for (var i = 0; i < combatLoadout.Weapons.Count; i++)
            {
                var weapon = combatLoadout.Weapons[i];
                if (weapon == null)
                {
                    continue;
                }

                var pathA = weapon.GetPathUpgradesTaken(WeaponUpgradePath.PathA);
                var pathB = weapon.GetPathUpgradesTaken(WeaponUpgradePath.PathB);
                WeaponUpgradePath path;
                if (pathA == pathB)
                {
                    path = Random.value < 0.5f ? WeaponUpgradePath.PathA : WeaponUpgradePath.PathB;
                }
                else
                {
                    path = pathA < pathB ? WeaponUpgradePath.PathA : WeaponUpgradePath.PathB;
                }

                weapon.ApplyPathUpgrade(path);
            }
        }

        private void TriggerAbyssalPulse()
        {
            if (playerMover == null)
            {
                return;
            }

            var colliders = Physics.OverlapSphere(playerMover.transform.position, AbyssalPulseRadius, enemyMask, QueryTriggerInteraction.Collide);
            resolvedTargets.Clear();
            for (var i = 0; i < colliders.Length; i++)
            {
                if (!CombatResolver.TryResolveUniqueHit(colliders[i], CombatTeam.Player, gameObject, resolvedTargets, out var resolvedHit))
                {
                    continue;
                }

                CombatResolver.TryApplyDamage(resolvedHit, gameObject, AbyssalPulseDamage, CombatTeam.Player);
            }

            RotLanternRadiusVisual.Spawn(playerMover.transform.position, AbyssalPulseRadius, 0.05f, 0.2f, new Color(0.86f, 0.18f, 0.18f, 0.34f), 0.35f, 1.1f, 15);
            PlaceholderWeaponVisual.Spawn(
                "AbyssalPulse",
                playerMover.transform.position + Vector3.up * 0.06f,
                new Vector3(AbyssalPulseRadius * 2.2f, AbyssalPulseRadius * 2.2f, 1f),
                Camera.main,
                new Color(0.94f, 0.22f, 0.24f, 0.28f),
                0.32f,
                1.12f,
                0f,
                spritePath: "Sprites/Effects/brine_surge",
                pixelsPerUnit: 256f,
                sortingOrder: 16,
                groundPlane: true);
        }

        private void HandleDashStarted(PlayerMover mover)
        {
            if (crashingSurgeEnabled)
            {
                crashingSurgeTimer = Mathf.Max(crashingSurgeTimer, CrashingSurgeDuration);
            }
        }

        private void HandleActiveActivated(ActiveAbilityRuntime ability)
        {
            if (crashingSurgeEnabled)
            {
                crashingSurgeTimer = Mathf.Max(crashingSurgeTimer, CrashingSurgeDuration);
            }
        }

        private void RefreshActiveSubscription()
        {
            var currentActive = combatLoadout != null ? combatLoadout.GetPrimaryActive() : null;
            if (ReferenceEquals(subscribedActive, currentActive))
            {
                return;
            }

            UnsubscribeActive();
            subscribedActive = currentActive;
            if (subscribedActive != null)
            {
                subscribedActive.Activated += HandleActiveActivated;
            }
        }

        private void UnsubscribeActive()
        {
            if (subscribedActive != null)
            {
                subscribedActive.Activated -= HandleActiveActivated;
                subscribedActive = null;
            }
        }

        private bool WasPlayerKill(GameObject source)
        {
            if (source == null)
            {
                return false;
            }

            if (source.GetComponentInParent<PlayerMover>() != null ||
                source.GetComponentInParent<PlayerCombatLoadout>() != null ||
                source.GetComponentInParent<ActiveAbilityRuntime>() != null)
            {
                return true;
            }

            var hurtbox = source.GetComponentInParent<Hurtbox>();
            return hurtbox != null && hurtbox.Team == CombatTeam.Player;
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
