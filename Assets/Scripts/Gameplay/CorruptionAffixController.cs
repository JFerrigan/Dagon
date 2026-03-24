using System.Collections.Generic;
using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CorruptionAffixController : MonoBehaviour
    {
        public enum CorruptionAffixKind
        {
            None,
            Graveburst,
            Rotwake,
            FrenziedVolley,
            HardenedHusk
        }

        private static readonly Color GraveburstColor = new(0.98f, 0.36f, 0.32f, 1f);
        private static readonly Color RotwakeColor = new(0.18f, 0.92f, 0.58f, 1f);
        private static readonly Color FrenziedVolleyColor = new(1f, 0.86f, 0.18f, 1f);
        private static readonly Color HardenedHuskColor = new(0.90f, 0.26f, 0.34f, 1f);

        private const float GraveburstRadius = 2.4f;
        private const float GraveburstDamage = 2.5f;
        private const float RotwakeSpawnInterval = 0.7f;
        private const float RotwakeMinimumTravel = 0.7f;
        private const float RotwakeRadius = 0.8f;
        private const float RotwakeDuration = 2.1f;
        private const float RotwakeTickDamage = 0.55f;
        private const float RotwakeTickInterval = 0.45f;
        private const float PulseHeightOffset = 0.08f;
        private const float PulseThickness = 0.18f;
        private const float PulseDuration = 0.42f;
        private const float PulseScaleMultiplier = 1.12f;
        private const float HardenedDamageMultiplier = 0.72f;
        private const float HardenedKnockbackMultiplier = 0.45f;

        private readonly HashSet<GameObject> resolvedRoots = new();

        private WorldProgressionDirector worldProgressionDirector;
        private Health health;
        private Hurtbox hurtbox;
        private KnockbackReceiver knockbackReceiver;
        private SpriteRenderer primaryRenderer;
        private SpriteRenderer glowRenderer;
        private Camera worldCamera;
        private ICorruptionVolleyAffixTarget volleyTarget;
        private CorruptionAffixKind affixKind;
        private Color baseTint = Color.white;
        private bool affixActive;
        private float rotwakeTimer;
        private Vector3 lastWakePosition;

        public CorruptionAffixKind AffixKind => affixKind;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (health != null)
            {
                health.Died -= HandleDied;
                health.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }

            SetAffixActive(false);
        }

        private void Update()
        {
            ResolveReferences();
            UpdateActivation();
            UpdateAffixVisual();
            if (affixActive && affixKind == CorruptionAffixKind.Rotwake)
            {
                UpdateRotwake();
            }
        }

        public void Configure(CorruptionAffixKind kind, Camera cameraReference)
        {
            affixKind = kind;
            worldCamera = cameraReference;
            ResolveReferences();
            lastWakePosition = transform.position;
            if (TryGetComponent<CorruptedVariantMarker>(out var marker))
            {
                marker.SetAffixKind(kind);
            }
        }

        private void ResolveReferences()
        {
            worldProgressionDirector ??= FindFirstObjectByType<WorldProgressionDirector>();
            health ??= GetComponent<Health>();
            hurtbox ??= GetComponent<Hurtbox>();
            knockbackReceiver ??= GetComponent<KnockbackReceiver>();
            volleyTarget ??= GetComponent<ICorruptionVolleyAffixTarget>();
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (primaryRenderer == null)
            {
                primaryRenderer = GetComponentInChildren<SpriteRenderer>();
                if (primaryRenderer != null)
                {
                    baseTint = primaryRenderer.color;
                    var glowTransform = primaryRenderer.transform.Find("CorruptionAffixGlow");
                    if (glowTransform == null)
                    {
                        var glowObject = new GameObject("CorruptionAffixGlow");
                        glowObject.transform.SetParent(primaryRenderer.transform, false);
                        glowObject.transform.localPosition = new Vector3(0f, 0f, -0.015f);
                        glowObject.transform.localScale = Vector3.one * 1.12f;
                        glowTransform = glowObject.transform;
                    }

                    glowRenderer = glowTransform.GetComponent<SpriteRenderer>();
                    if (glowRenderer == null)
                    {
                        glowRenderer = glowTransform.gameObject.AddComponent<SpriteRenderer>();
                    }

                    glowRenderer.enabled = false;
                    glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    glowRenderer.receiveShadows = false;
                }
            }
        }

        private void UpdateActivation()
        {
            var shouldBeActive =
                affixKind != CorruptionAffixKind.None &&
                worldProgressionDirector != null &&
                worldProgressionDirector.IsPositionCorrupted(transform.position);

            if (shouldBeActive == affixActive)
            {
                return;
            }

            SetAffixActive(shouldBeActive);
        }

        private void SetAffixActive(bool active)
        {
            affixActive = active;
            if (!active)
            {
                rotwakeTimer = 0f;
                lastWakePosition = transform.position;
            }

            switch (affixKind)
            {
                case CorruptionAffixKind.FrenziedVolley:
                    volleyTarget?.SetCorruptionVolleyActive(active);
                    break;
                case CorruptionAffixKind.HardenedHusk:
                    if (health != null)
                    {
                        health.SetExternalIncomingDamageMultiplier(active ? HardenedDamageMultiplier : 1f);
                    }

                    knockbackReceiver?.SetExternalStrengthMultiplier(active ? HardenedKnockbackMultiplier : 1f);
                    break;
            }
        }

        private void UpdateRotwake()
        {
            rotwakeTimer -= Time.deltaTime;
            var planarOffset = transform.position - lastWakePosition;
            planarOffset.y = 0f;
            if (rotwakeTimer > 0f || planarOffset.sqrMagnitude < RotwakeMinimumTravel * RotwakeMinimumTravel)
            {
                return;
            }

            EnemyHazardZone.SpawnForTeam(
                transform.position,
                RotwakeRadius,
                RotwakeDuration,
                RotwakeTickDamage,
                RotwakeTickInterval,
                worldCamera,
                new Color(RotwakeColor.r, RotwakeColor.g, RotwakeColor.b, 0.42f),
                CombatTeam.Player,
                gameObject,
                "CorruptionWake");

            lastWakePosition = transform.position;
            rotwakeTimer = RotwakeSpawnInterval;
        }

        private void UpdateAffixVisual()
        {
            if (primaryRenderer == null || glowRenderer == null)
            {
                return;
            }

            if (!affixActive || affixKind == CorruptionAffixKind.None)
            {
                primaryRenderer.color = baseTint;
                glowRenderer.enabled = false;
                return;
            }

            var color = ResolveAffixColor(affixKind);
            primaryRenderer.color = Color.Lerp(baseTint, color, 0.2f);
            glowRenderer.enabled = true;
            glowRenderer.sprite = primaryRenderer.sprite;
            glowRenderer.flipX = primaryRenderer.flipX;
            glowRenderer.flipY = primaryRenderer.flipY;
            glowRenderer.sortingOrder = primaryRenderer.sortingOrder + 2;
            var pulse = 0.26f + ((Mathf.Sin(Time.time * 4.2f) + 1f) * 0.5f * 0.24f);
            glowRenderer.color = new Color(color.r, color.g, color.b, pulse);
        }

        private void HandleDied(Health _, GameObject source)
        {
            if (affixKind != CorruptionAffixKind.Graveburst || !affixActive)
            {
                return;
            }

            RotLanternRadiusVisual.Spawn(
                transform.position,
                GraveburstRadius,
                PulseHeightOffset,
                PulseThickness,
                GraveburstColor,
                PulseDuration,
                PulseScaleMultiplier,
                12);

            resolvedRoots.Clear();
            var sourceTeam = hurtbox != null ? hurtbox.Team : CombatTeam.Enemy;
            var colliders = Physics.OverlapSphere(transform.position, GraveburstRadius, ~0, QueryTriggerInteraction.Collide);
            for (var index = 0; index < colliders.Length; index++)
            {
                if (!CombatResolver.TryResolveUniqueHit(colliders[index], sourceTeam, gameObject, resolvedRoots, out var result))
                {
                    continue;
                }

                if (result.TargetTeam != CombatTeam.Player)
                {
                    continue;
                }

                CombatResolver.TryApplyDamage(result, gameObject, GraveburstDamage, sourceTeam);
            }
        }

        private static Color ResolveAffixColor(CorruptionAffixKind kind)
        {
            return kind switch
            {
                CorruptionAffixKind.Graveburst => GraveburstColor,
                CorruptionAffixKind.Rotwake => RotwakeColor,
                CorruptionAffixKind.FrenziedVolley => FrenziedVolleyColor,
                CorruptionAffixKind.HardenedHusk => HardenedHuskColor,
                _ => Color.white
            };
        }
    }

    public interface ICorruptionVolleyAffixTarget
    {
        void SetCorruptionVolleyActive(bool active);
    }
}
