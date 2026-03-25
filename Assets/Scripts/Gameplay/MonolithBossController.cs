using System.Collections;
using System.Collections.Generic;
using Dagon.Core;
using Dagon.Rendering;
using Dagon.UI;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MonolithBossController : MonoBehaviour, IDamageable, IBossHealthDisplayOverride
    {
        private const string WideLeechSpritePath = "Sprites/Enemies/wide_leech";
        private const string TallLeechSpritePath = "Sprites/Enemies/tall_leech";
        private const float PhaseOneHealthRatio = 0.5f;
        private const float RelocationDistance = 30f;
        private const float RelocationDistanceJitter = 3.5f;
        private const float MinimumRelocationDistance = 20f;
        private const float CorruptionSafetyBuffer = 4f;
        private const float SinkDepth = 7f;
        private const float SinkDuration = 0.42f;
        private const float HiddenDuration = 0.3f;
        private const float RiseDuration = 0.38f;
        private const float PressureCadenceMultiplier = 3f;
        private const int PressureWideCapBonus = 8;
        private const int PressureTallCapBonus = 4;
        private const float AuraPulseHeightOffset = 0.06f;
        private const float AuraPulseDuration = 0.55f;
        private const float AuraPulseScaleMultiplier = 1.08f;

        private static readonly Color SpeedAuraColor = new(0.28f, 0.64f, 1f, 1f);
        private static readonly Color BulwarkAuraColor = new(1f, 0.28f, 0.28f, 1f);
        private static readonly Color MendAuraColor = new(0.28f, 0.95f, 0.42f, 1f);
        private static readonly Color VolleyAuraColor = new(1f, 0.88f, 0.22f, 1f);
        private static readonly Color DeadHuskColor = new(0.42f, 0.42f, 0.42f, 1f);

        private enum AuraType
        {
            Speed,
            Bulwark,
            Mend,
            Volley
        }

        private enum PhaseState
        {
            PhaseOne,
            Transitioning,
            RelocatedPressure,
            PhaseTwo,
            DeadHusk
        }

        [SerializeField] private Transform target;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private HarpoonProjectile projectilePrefab;
        [SerializeField] private float summonRadius = 5.2f;
        [SerializeField] private float wideSummonCooldown = 2.4f;
        [SerializeField] private float tallSummonCooldown = 4.9f;
        [SerializeField] private int maxWideLeeches = 6;
        [SerializeField] private int maxTallLeeches = 3;
        [SerializeField] private float auraRadius = 8.5f;
        [SerializeField] private float auraSwapInterval = 13f;
        [SerializeField] private float speedAuraMoveMultiplier = 1.45f;
        [SerializeField] private float bulwarkIncomingDamageMultiplier = 0.65f;
        [SerializeField] private float mendHealPerTick = 0.85f;
        [SerializeField] private float mendTickInterval = 1.1f;
        [SerializeField] private int volleyProjectileMultiplier = 2;
        [SerializeField] private float volleyFallbackCadenceMultiplier = 1.55f;
        [SerializeField] private float auraRingThickness = 0.22f;

        private readonly List<GameObject> activeWideLeeches = new();
        private readonly List<GameObject> activeTallLeeches = new();
        private readonly HashSet<EnemyAuraBuffReceiver> buffedEnemies = new();
        private readonly HashSet<EnemyAuraBuffReceiver> trackedEnemies = new();
        private readonly HashSet<EnemyAuraBuffReceiver> auraFrameTargets = new();
        private readonly HashSet<GameObject> resolvedRoots = new();

        private Sprite wideLeechSprite;
        private Sprite tallLeechSprite;
        private Health bossHealth;
        private Hurtbox hurtbox;
        private CapsuleCollider bossCollider;
        private BodyBlocker bodyBlocker;
        private EnemyDeathRewards deathRewards;
        private KnockbackReceiver knockbackReceiver;
        private WorldProgressionDirector worldProgressionDirector;
        private RunStateManager runStateManager;
        private Transform visualsRoot;
        private SpriteRenderer primaryRenderer;
        private SpriteRenderer glowRenderer;
        private Vector3 baseVisualLocalPosition;
        private Color baseTint = Color.white;

        private float wideSummonTimer;
        private float tallSummonTimer;
        private float auraSwapTimer;
        private float damageMultiplier = 1f;
        private float cadenceMultiplier = 1f;
        private float configuredWideSummonCooldown;
        private float configuredTallSummonCooldown;
        private int configuredMaxWideLeeches;
        private int configuredMaxTallLeeches;
        private float phaseOneMaxHealth;
        private float phaseTwoMaxHealth;
        private float phaseCurrentHealth;

        private AuraType currentAura;
        private PhaseState phaseState;
        private Coroutine transitionRoutine;

        public float DisplayedCurrentHealth => Mathf.Max(0f, phaseCurrentHealth);
        public float DisplayedMaxHealth => phaseState switch
        {
            PhaseState.PhaseOne => Mathf.Max(0.01f, phaseOneMaxHealth),
            PhaseState.Transitioning => Mathf.Max(0.01f, phaseTwoMaxHealth),
            PhaseState.RelocatedPressure => Mathf.Max(0.01f, phaseTwoMaxHealth),
            PhaseState.PhaseTwo => Mathf.Max(0.01f, phaseTwoMaxHealth),
            _ => Mathf.Max(0.01f, phaseTwoMaxHealth)
        };

        public bool IsBossHealthVisible =>
            phaseState == PhaseState.PhaseOne ||
            phaseState == PhaseState.Transitioning ||
            phaseState == PhaseState.RelocatedPressure ||
            phaseState == PhaseState.PhaseTwo;

        private void Awake()
        {
            wideLeechSprite = RuntimeSpriteLibrary.LoadSprite(WideLeechSpritePath, 64f);
            tallLeechSprite = RuntimeSpriteLibrary.LoadSprite(TallLeechSpritePath, 64f);
            ResolveReferences();
            ResolveVisuals();
            ChooseNextAura(initial: true);
        }

        private void Update()
        {
            ResolveReferences();
            ResolveVisuals();
            CleanupDestroyedLeeches(activeWideLeeches);
            CleanupDestroyedLeeches(activeTallLeeches);

            if (phaseState == PhaseState.DeadHusk)
            {
                return;
            }

            if (phaseState != PhaseState.Transitioning)
            {
                wideSummonTimer -= Time.deltaTime;
                tallSummonTimer -= Time.deltaTime;
                auraSwapTimer -= Time.deltaTime;

                if (activeWideLeeches.Count < maxWideLeeches && wideSummonTimer <= 0f)
                {
                    SpawnWideLeech();
                    wideSummonTimer = wideSummonCooldown;
                }

                if (activeTallLeeches.Count < maxTallLeeches && tallSummonTimer <= 0f)
                {
                    SpawnTallLeech();
                    tallSummonTimer = tallSummonCooldown;
                }

                if (auraSwapTimer <= 0f)
                {
                    ChooseNextAura();
                }

                UpdateAuraVisuals();
                ApplyAuraToNearbyEnemies();
            }

            if (phaseState == PhaseState.RelocatedPressure && IsTargetWithinAuraRadius())
            {
                ExitRelocatedPressure();
            }
        }

        private void OnDisable()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            ClearAllBuffs();
            SetMonolithPresentationActive(true);
        }

        public void Configure(
            Transform newTarget,
            Camera cameraReference,
            HarpoonProjectile leechProjectilePrefab,
            float newWideSummonCooldown,
            float newTallSummonCooldown,
            int newMaxWideLeeches,
            int newMaxTallLeeches)
        {
            ResolveReferences();
            target = newTarget;
            worldCamera = cameraReference;
            projectilePrefab = leechProjectilePrefab;
            worldProgressionDirector ??= FindFirstObjectByType<WorldProgressionDirector>();
            configuredWideSummonCooldown = Mathf.Max(0.2f, newWideSummonCooldown);
            configuredTallSummonCooldown = Mathf.Max(0.2f, newTallSummonCooldown);
            configuredMaxWideLeeches = Mathf.Max(1, newMaxWideLeeches);
            configuredMaxTallLeeches = Mathf.Max(1, newMaxTallLeeches);
            ApplyNormalSummonPressure();
            wideSummonTimer = 0.9f;
            tallSummonTimer = 1.8f;
            auraSwapTimer = 0.4f;

            if (bossHealth != null)
            {
                var totalHealth = Mathf.Max(1f, bossHealth.MaxHealth);
                phaseOneMaxHealth = totalHealth * PhaseOneHealthRatio;
                phaseTwoMaxHealth = totalHealth - phaseOneMaxHealth;
                phaseCurrentHealth = phaseOneMaxHealth;
            }

            phaseState = PhaseState.PhaseOne;
            if (hurtbox != null)
            {
                hurtbox.SetDamageableOverride(this);
            }
        }

        public void ApplyCorruptionModifiers(float summonCadenceMultiplier)
        {
            cadenceMultiplier = Mathf.Max(0.1f, summonCadenceMultiplier);
            damageMultiplier = 1.3f;
            configuredWideSummonCooldown = Mathf.Max(0.2f, configuredWideSummonCooldown / cadenceMultiplier);
            configuredTallSummonCooldown = Mathf.Max(0.2f, configuredTallSummonCooldown / cadenceMultiplier);
            configuredMaxWideLeeches += 2;
            configuredMaxTallLeeches += 1;
            auraSwapInterval = Mathf.Max(2.5f, auraSwapInterval / Mathf.Max(1f, cadenceMultiplier * 0.85f));
            ApplyNormalSummonPressure();
        }

        public void ApplyDamage(float amount, GameObject source)
        {
            if (phaseState == PhaseState.DeadHusk || phaseState == PhaseState.Transitioning || phaseState == PhaseState.RelocatedPressure || amount <= 0f)
            {
                return;
            }

            phaseCurrentHealth = Mathf.Max(0f, phaseCurrentHealth - amount);
            if (phaseState == PhaseState.PhaseOne)
            {
                if (phaseCurrentHealth > 0f)
                {
                    return;
                }

                BeginPhaseTransition();
                return;
            }

            if (phaseCurrentHealth > 0f || bossHealth == null)
            {
                return;
            }

            EnterDeadHuskState(source);
        }

        private void ResolveReferences()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                target = player != null ? player.transform : null;
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (projectilePrefab == null && worldCamera != null)
            {
                projectilePrefab = RuntimeOrbProjectileFactory.Create(worldCamera);
            }

            bossHealth ??= GetComponent<Health>();
            hurtbox ??= GetComponent<Hurtbox>();
            bossCollider ??= GetComponent<CapsuleCollider>();
            bodyBlocker ??= GetComponent<BodyBlocker>();
            deathRewards ??= GetComponent<EnemyDeathRewards>();
            knockbackReceiver ??= GetComponent<KnockbackReceiver>();
            runStateManager ??= FindFirstObjectByType<RunStateManager>();
            knockbackReceiver?.SetSuppressed(true);
        }

        private void ResolveVisuals()
        {
            if (visualsRoot == null)
            {
                visualsRoot = transform.Find("Visuals");
                if (visualsRoot != null)
                {
                    baseVisualLocalPosition = visualsRoot.localPosition;
                }
            }

            if (primaryRenderer == null && visualsRoot != null)
            {
                primaryRenderer = visualsRoot.GetComponent<SpriteRenderer>();
                if (primaryRenderer != null)
                {
                    baseTint = primaryRenderer.color;
                }
            }

            if (primaryRenderer != null && glowRenderer == null)
            {
                var glowTransform = primaryRenderer.transform.Find("MonolithAuraGlow");
                if (glowTransform == null)
                {
                    var glowObject = new GameObject("MonolithAuraGlow");
                    glowObject.transform.SetParent(primaryRenderer.transform, false);
                    glowObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);
                    glowObject.transform.localScale = Vector3.one * 1.1f;
                    glowTransform = glowObject.transform;
                }

                glowRenderer = glowTransform.GetComponent<SpriteRenderer>();
                if (glowRenderer == null)
                {
                    glowRenderer = glowTransform.gameObject.AddComponent<SpriteRenderer>();
                }

                glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                glowRenderer.receiveShadows = false;
            }

        }

        private void ChooseNextAura(bool initial = false)
        {
            var nextAura = currentAura;
            if (initial)
            {
                nextAura = AuraType.Speed;
            }
            else
            {
                var attempts = 0;
                while (nextAura == currentAura && attempts < 8)
                {
                    nextAura = (AuraType)Random.Range(0, 4);
                    attempts += 1;
                }
            }

            currentAura = nextAura;
            auraSwapTimer = auraSwapInterval;
            TriggerAuraPulse();
            UpdateAuraVisuals();
            RefreshBuffedEnemies();
        }

        private void UpdateAuraVisuals()
        {
            var auraColor = ResolveAuraColor(currentAura);
            if (primaryRenderer != null)
            {
                primaryRenderer.color = Color.Lerp(baseTint, auraColor, 0.35f);
            }

            if (glowRenderer != null && primaryRenderer != null)
            {
                glowRenderer.sprite = primaryRenderer.sprite;
                glowRenderer.flipX = primaryRenderer.flipX;
                glowRenderer.flipY = primaryRenderer.flipY;
                glowRenderer.sortingOrder = primaryRenderer.sortingOrder + 1;
                var pulse = 0.22f + (Mathf.Sin(Time.time * 3.8f) * 0.14f);
                glowRenderer.color = new Color(auraColor.r, auraColor.g, auraColor.b, Mathf.Clamp01(pulse));
            }

        }

        private void ApplyAuraToNearbyEnemies()
        {
            if (phaseState == PhaseState.Transitioning)
            {
                return;
            }

            auraFrameTargets.Clear();
            resolvedRoots.Clear();
            var colliders = Physics.OverlapSphere(transform.position, auraRadius, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < colliders.Length; i++)
            {
                var hurtboxTarget = colliders[i] != null ? colliders[i].GetComponentInParent<Hurtbox>() : null;
                if (hurtboxTarget == null || hurtboxTarget.Team != CombatTeam.Enemy || hurtboxTarget.gameObject == gameObject)
                {
                    continue;
                }

                if (!resolvedRoots.Add(hurtboxTarget.gameObject))
                {
                    continue;
                }

                var receiver = hurtboxTarget.GetComponent<EnemyAuraBuffReceiver>() ?? hurtboxTarget.gameObject.AddComponent<EnemyAuraBuffReceiver>();
                receiver.ApplyAura(
                    ToReceiverAura(currentAura),
                    speedAuraMoveMultiplier,
                    bulwarkIncomingDamageMultiplier,
                    mendHealPerTick,
                    mendTickInterval,
                    volleyProjectileMultiplier,
                    volleyFallbackCadenceMultiplier);
                auraFrameTargets.Add(receiver);
                trackedEnemies.Add(receiver);
            }

            buffedEnemies.RemoveWhere(receiver =>
            {
                if (receiver == null)
                {
                    trackedEnemies.Remove(receiver);
                    return true;
                }

                return !auraFrameTargets.Contains(receiver);
            });

            foreach (var receiver in auraFrameTargets)
            {
                buffedEnemies.Add(receiver);
            }
        }

        private void RefreshBuffedEnemies()
        {
            foreach (var receiver in buffedEnemies)
            {
                if (receiver == null)
                {
                    continue;
                }

                receiver.ApplyAura(
                    ToReceiverAura(currentAura),
                    speedAuraMoveMultiplier,
                    bulwarkIncomingDamageMultiplier,
                    mendHealPerTick,
                    mendTickInterval,
                    volleyProjectileMultiplier,
                    volleyFallbackCadenceMultiplier);
            }
        }

        private void ClearAllBuffs()
        {
            foreach (var receiver in trackedEnemies)
            {
                if (receiver == null)
                {
                    continue;
                }

                receiver.ClearAura();
            }

            buffedEnemies.Clear();
            trackedEnemies.Clear();
            if (primaryRenderer != null)
            {
                primaryRenderer.color = baseTint;
            }
        }

        private void EnterDeadHuskState(GameObject source)
        {
            if (phaseState == PhaseState.DeadHusk)
            {
                return;
            }

            phaseState = PhaseState.DeadHusk;
            phaseCurrentHealth = 0f;
            ClearAllBuffs();
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            if (hurtbox != null)
            {
                hurtbox.SetDamageableOverride(null);
                hurtbox.enabled = false;
            }

            if (bossCollider != null)
            {
                bossCollider.enabled = false;
            }

            if (bodyBlocker != null)
            {
                bodyBlocker.SetSuppressed(false);
            }

            knockbackReceiver?.SetSuppressed(true);
            ApplyDeadHuskVisuals();

            if (bossHealth != null)
            {
                bossHealth.SetDestroyOnDeath(false);
                bossHealth.ApplyDamage(bossHealth.CurrentHealth, source != null ? source : gameObject);
                runStateManager?.NotifyBossDefeated(bossHealth, source != null ? source : gameObject);
            }
        }

        private void ApplyDeadHuskVisuals()
        {
            if (visualsRoot != null)
            {
                visualsRoot.localPosition = baseVisualLocalPosition;
            }

            if (primaryRenderer != null)
            {
                primaryRenderer.enabled = true;
                primaryRenderer.color = Color.Lerp(baseTint, DeadHuskColor, 0.92f);
            }

            if (glowRenderer != null)
            {
                glowRenderer.enabled = false;
            }
        }

        private void BeginPhaseTransition()
        {
            if (phaseState != PhaseState.PhaseOne)
            {
                return;
            }

            phaseState = PhaseState.Transitioning;
            phaseCurrentHealth = 0f;
            ClearAllBuffs();
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(HandlePhaseTransition());
        }

        private IEnumerator HandlePhaseTransition()
        {
            var refillDuration = SinkDuration + HiddenDuration + RiseDuration;
            var refillElapsed = 0f;

            if (bossCollider != null)
            {
                bossCollider.enabled = false;
            }

            if (bodyBlocker != null)
            {
                bodyBlocker.SetSuppressed(true);
            }

            var sinkElapsed = 0f;
            while (sinkElapsed < SinkDuration)
            {
                var delta = Time.deltaTime;
                sinkElapsed += delta;
                refillElapsed += delta;
                if (visualsRoot != null)
                {
                    var sinkProgress = SinkDuration > 0f ? Mathf.Clamp01(sinkElapsed / SinkDuration) : 1f;
                    visualsRoot.localPosition = Vector3.Lerp(baseVisualLocalPosition, baseVisualLocalPosition + Vector3.down * SinkDepth, sinkProgress);
                }

                phaseCurrentHealth = Mathf.Lerp(0f, phaseTwoMaxHealth, Mathf.Clamp01(refillElapsed / refillDuration));
                yield return null;
            }

            if (visualsRoot != null)
            {
                visualsRoot.localPosition = baseVisualLocalPosition + Vector3.down * SinkDepth;
            }

            SetMonolithPresentationActive(false);
            var hiddenElapsed = 0f;
            while (hiddenElapsed < HiddenDuration)
            {
                var delta = Time.deltaTime;
                hiddenElapsed += delta;
                refillElapsed += Time.deltaTime;
                phaseCurrentHealth = Mathf.Lerp(0f, phaseTwoMaxHealth, Mathf.Clamp01(refillElapsed / refillDuration));
                yield return null;
            }

            transform.position = BuildRelocationPosition();
            if (visualsRoot != null)
            {
                visualsRoot.localPosition = baseVisualLocalPosition + Vector3.down * SinkDepth;
            }

            phaseCurrentHealth = phaseTwoMaxHealth;
            phaseState = PhaseState.RelocatedPressure;
            ApplyPressureSummonTuning();
            wideSummonTimer = 0.08f;
            tallSummonTimer = 0.18f;
            SetMonolithPresentationActive(true);
            var riseElapsed = 0f;
            while (riseElapsed < RiseDuration)
            {
                var delta = Time.deltaTime;
                riseElapsed += delta;
                refillElapsed += delta;
                if (visualsRoot != null)
                {
                    var riseProgress = RiseDuration > 0f ? Mathf.Clamp01(riseElapsed / RiseDuration) : 1f;
                    visualsRoot.localPosition = Vector3.Lerp(baseVisualLocalPosition + Vector3.down * SinkDepth, baseVisualLocalPosition, riseProgress);
                }

                phaseCurrentHealth = Mathf.Lerp(0f, phaseTwoMaxHealth, Mathf.Clamp01(refillElapsed / refillDuration));
                yield return null;
            }
            if (visualsRoot != null)
            {
                visualsRoot.localPosition = baseVisualLocalPosition;
            }
            phaseCurrentHealth = phaseTwoMaxHealth;
            transitionRoutine = null;
        }

        private IEnumerator AnimateSinkAndRise(Vector3 from, Vector3 to, float duration)
        {
            if (visualsRoot == null)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                visualsRoot.localPosition = Vector3.Lerp(from, to, progress);
                yield return null;
            }

            visualsRoot.localPosition = to;
        }

        private void SetMonolithPresentationActive(bool active)
        {
            if (primaryRenderer != null)
            {
                primaryRenderer.enabled = active;
            }

            if (glowRenderer != null)
            {
                glowRenderer.enabled = active && phaseState != PhaseState.DeadHusk;
            }

            if (bossCollider != null)
            {
                bossCollider.enabled = active;
            }

            if (bodyBlocker != null)
            {
                bodyBlocker.SetSuppressed(!active);
            }
        }

        private bool IsTargetWithinAuraRadius()
        {
            if (target == null)
            {
                return false;
            }

            var offset = target.position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= auraRadius * auraRadius;
        }

        private void ExitRelocatedPressure()
        {
            if (phaseState != PhaseState.RelocatedPressure)
            {
                return;
            }

            phaseState = PhaseState.PhaseTwo;
            ApplyNormalSummonPressure();
        }

        private void TriggerAuraPulse()
        {
            if (phaseState == PhaseState.DeadHusk)
            {
                return;
            }

            RotLanternRadiusVisual.Spawn(
                transform.position,
                auraRadius,
                AuraPulseHeightOffset,
                auraRingThickness,
                ResolveAuraColor(currentAura),
                AuraPulseDuration,
                AuraPulseScaleMultiplier,
                18);
        }

        private void ApplyNormalSummonPressure()
        {
            wideSummonCooldown = configuredWideSummonCooldown;
            tallSummonCooldown = configuredTallSummonCooldown;
            maxWideLeeches = configuredMaxWideLeeches;
            maxTallLeeches = configuredMaxTallLeeches;
        }

        private void ApplyPressureSummonTuning()
        {
            wideSummonCooldown = Mathf.Max(0.08f, configuredWideSummonCooldown / PressureCadenceMultiplier);
            tallSummonCooldown = Mathf.Max(0.08f, configuredTallSummonCooldown / PressureCadenceMultiplier);
            maxWideLeeches = configuredMaxWideLeeches + PressureWideCapBonus;
            maxTallLeeches = configuredMaxTallLeeches + PressureTallCapBonus;
        }

        private Vector3 BuildRelocationPosition()
        {
            var anchor = target != null ? target.position : transform.position;
            worldProgressionDirector ??= FindFirstObjectByType<WorldProgressionDirector>();

            var corruptionOrigin = worldProgressionDirector != null ? worldProgressionDirector.CorruptionOrigin : Vector3.zero;
            var corruptionRadius = worldProgressionDirector != null ? worldProgressionDirector.CurrentCorruptionRadius : 0f;
            var bestPosition = ResolveFallbackRelocation(anchor, corruptionOrigin, corruptionRadius);
            var bestOutsideScore = float.MinValue;
            var bestFallbackScore = float.MinValue;

            for (var attempt = 0; attempt < 12; attempt++)
            {
                var direction = Random.insideUnitCircle;
                if (direction.sqrMagnitude <= 0.001f)
                {
                    direction = Vector2.right;
                }

                direction.Normalize();
                var distance = RelocationDistance + Random.Range(-RelocationDistanceJitter, RelocationDistanceJitter);
                var candidate = anchor + new Vector3(direction.x, 0f, direction.y) * distance;
                candidate.y = transform.position.y;
                var offset = candidate - transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude < MinimumRelocationDistance * MinimumRelocationDistance)
                {
                    continue;
                }

                var candidateDistanceFromOrigin = Vector3.Distance(Flatten(candidate), corruptionOrigin);
                var outsideCorruption = corruptionRadius > 0.01f && candidateDistanceFromOrigin <= corruptionRadius - CorruptionSafetyBuffer;
                if (outsideCorruption)
                {
                    var outsideScore = corruptionRadius - candidateDistanceFromOrigin;
                    if (outsideScore > bestOutsideScore)
                    {
                        bestOutsideScore = outsideScore;
                        bestPosition = candidate;
                    }

                    continue;
                }

                if (candidateDistanceFromOrigin > bestFallbackScore)
                {
                    bestFallbackScore = candidateDistanceFromOrigin;
                    bestPosition = candidate;
                }
            }

            return bestPosition;
        }

        private Vector3 ResolveFallbackRelocation(Vector3 anchor, Vector3 corruptionOrigin, float corruptionRadius)
        {
            var away = Flatten(anchor) - corruptionOrigin;
            if (away.sqrMagnitude <= 0.001f)
            {
                away = Flatten(transform.position) - corruptionOrigin;
            }

            if (away.sqrMagnitude <= 0.001f)
            {
                away = Vector3.forward;
            }

            away.Normalize();
            var currentDistanceFromOrigin = Vector3.Distance(Flatten(transform.position), corruptionOrigin);
            var desiredDistance = Mathf.Min(corruptionRadius - CorruptionSafetyBuffer, currentDistanceFromOrigin - 8f);
            if (desiredDistance <= 0.5f)
            {
                desiredDistance = Mathf.Max(0.5f, corruptionRadius * 0.5f);
            }
            var fallback = corruptionOrigin + (away * desiredDistance);
            fallback.y = transform.position.y;

            var offset = fallback - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude >= MinimumRelocationDistance * MinimumRelocationDistance)
            {
                return fallback;
            }

            var fartherDistance = Mathf.Min(desiredDistance, currentDistanceFromOrigin - MinimumRelocationDistance);
            if (fartherDistance <= 0.5f)
            {
                fartherDistance = desiredDistance;
            }
            var fartherFallback = corruptionOrigin + (away * fartherDistance);
            fartherFallback.y = transform.position.y;
            return fartherFallback;
        }

        private static Vector3 Flatten(Vector3 position)
        {
            return new Vector3(position.x, 0f, position.z);
        }

        private static EnemyAuraBuffReceiver.AuraKind ToReceiverAura(AuraType auraType)
        {
            return auraType switch
            {
                AuraType.Speed => EnemyAuraBuffReceiver.AuraKind.Speed,
                AuraType.Bulwark => EnemyAuraBuffReceiver.AuraKind.Bulwark,
                AuraType.Mend => EnemyAuraBuffReceiver.AuraKind.Mend,
                AuraType.Volley => EnemyAuraBuffReceiver.AuraKind.Volley,
                _ => EnemyAuraBuffReceiver.AuraKind.None
            };
        }

        private static Color ResolveAuraColor(AuraType auraType)
        {
            return auraType switch
            {
                AuraType.Speed => SpeedAuraColor,
                AuraType.Bulwark => BulwarkAuraColor,
                AuraType.Mend => MendAuraColor,
                AuraType.Volley => VolleyAuraColor,
                _ => Color.white
            };
        }

        private void SpawnWideLeech()
        {
            if (wideLeechSprite == null)
            {
                return;
            }

            var leech = CreateBaseSummon("WideLeech", BuildSummonPosition(), 4f, new Vector3(0f, 0.21f, 0f), 0.23f, 0.45f, wideLeechSprite, new Vector3(0.85f, 0.85f, 1f), new Vector3(0f, 0.04f, 0f), new Vector3(0f, 0.52f, 0f));
            var contactDamage = leech.AddComponent<ContactDamage>();
            contactDamage.Configure(2f * damageMultiplier);
            var chaser = leech.AddComponent<SimpleEnemyChaser>();
            chaser.Configure(4.1f, 0.32f);
            var rewards = leech.AddComponent<EnemyDeathRewards>();
            rewards.Configure(0, 0f);
            activeWideLeeches.Add(leech);
        }

        private void SpawnTallLeech()
        {
            if (tallLeechSprite == null || projectilePrefab == null)
            {
                return;
            }

            var leech = CreateBaseSummon("TallLeech", BuildSummonPosition(), 5f, new Vector3(0f, 0.95f, 0f), 0.4f, 1.9f, tallLeechSprite, new Vector3(1.45f, 1.45f, 1f), Vector3.zero, new Vector3(0f, 1.8f, 0f));
            var shooter = leech.AddComponent<TallLeechShooter>();
            shooter.Configure(target, projectilePrefab, worldCamera, 9f, 2.1f, 7.4f, 1f);
            shooter.ApplyCorruptionModifiers(damageMultiplier, cadenceMultiplier);
            var rewards = leech.AddComponent<EnemyDeathRewards>();
            rewards.Configure(0, 0f);
            activeTallLeeches.Add(leech);
        }

        private GameObject CreateBaseSummon(
            string objectName,
            Vector3 position,
            float maxHealth,
            Vector3 colliderCenter,
            float colliderRadius,
            float colliderHeight,
            Sprite sprite,
            Vector3 visualScale,
            Vector3 visualOffset,
            Vector3 healthBarOffset)
        {
            var summon = new GameObject(objectName);
            summon.transform.position = position;
            summon.transform.SetParent(transform.parent, true);

            var collider = summon.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            collider.center = colliderCenter;
            collider.radius = colliderRadius;
            collider.height = colliderHeight;
            summon.AddComponent<BodyBlocker>().Configure(
                BodyBlocker.BodyTeam.Enemy,
                Mathf.Max(0.28f, colliderRadius * 0.85f),
                colliderHeight,
                0.95f,
                true,
                true);

            var rigidbody = summon.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            var health = summon.AddComponent<Health>();
            health.SetMaxHealth(maxHealth, true);
            summon.AddComponent<Hurtbox>().Configure(CombatTeam.Enemy, health);
            summon.AddComponent<KnockbackReceiver>().Configure(0.8f, 18f, 4.8f);

            var healthBar = summon.AddComponent<EnemyHealthBar>();
            healthBar.Configure(worldCamera, healthBarOffset, true, 2.25f);

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(summon.transform, false);
            visuals.transform.localPosition = visualOffset;
            visuals.transform.localScale = visualScale;

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 12;
            renderer.color = Color.white;

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);
            CombatVolumeAlignment.TryAlignCapsuleToSpriteCenter(summon.transform, collider);

            return summon;
        }

        private Vector3 BuildSummonPosition()
        {
            var direction = Random.insideUnitCircle;
            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector2.right;
            }

            direction.Normalize();
            var position = transform.position + new Vector3(direction.x, 0f, direction.y) * summonRadius;
            position.y = target != null ? target.position.y : 0f;
            return position;
        }

        private static void CleanupDestroyedLeeches(List<GameObject> leeches)
        {
            for (var index = leeches.Count - 1; index >= 0; index--)
            {
                if (leeches[index] == null)
                {
                    leeches.RemoveAt(index);
                }
            }
        }
    }
}
