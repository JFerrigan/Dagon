using System.Collections.Generic;
using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EnemyAuraBuffReceiver : MonoBehaviour
    {
        private const float AuraLingerDuration = 2.5f;
        private static readonly Color SpeedAuraColor = new(0.28f, 0.64f, 1f, 1f);
        private static readonly Color BulwarkAuraColor = new(1f, 0.28f, 0.28f, 1f);
        private static readonly Color MendAuraColor = new(0.28f, 0.95f, 0.42f, 1f);
        private static readonly Color VolleyAuraColor = new(1f, 0.88f, 0.22f, 1f);

        public enum AuraKind
        {
            None,
            Speed,
            Bulwark,
            Mend,
            Volley
        }

        private readonly List<IAuraMoveSpeedTarget> moveTargets = new();
        private readonly List<IAuraCadenceTarget> cadenceTargets = new();
        private readonly List<IAuraProjectileTarget> projectileTargets = new();
        private readonly List<MonoBehaviour> behaviourCache = new();

        private Health health;
        private Hurtbox hurtbox;
        private ContactDamage contactDamage;
        private SpriteRenderer primaryRenderer;
        private SpriteRenderer glowRenderer;
        private Color baseTint = Color.white;
        private AuraKind activeAura;
        private float healPerTick;
        private float healTickInterval;
        private float healTimer;
        private float auraLingerRemaining;

        public Hurtbox Hurtbox
        {
            get
            {
                ResolveReferences();
                return hurtbox;
            }
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            UpdateGlow();

            if (activeAura == AuraKind.None)
            {
                return;
            }

            auraLingerRemaining = Mathf.Max(0f, auraLingerRemaining - Time.deltaTime);
            if (auraLingerRemaining <= 0f)
            {
                ClearAura();
                return;
            }

            if (activeAura != AuraKind.Mend || health == null || healPerTick <= 0f)
            {
                return;
            }

            healTimer -= Time.deltaTime;
            if (healTimer > 0f)
            {
                return;
            }

            healTimer = healTickInterval;
            health.Restore(healPerTick);
        }

        private void OnDisable()
        {
            ClearAura();
        }

        public void ApplyAura(
            AuraKind auraKind,
            float moveSpeedMultiplier,
            float damageTakenMultiplier,
            float healingPerTick,
            float healingTickInterval,
            int projectileMultiplier,
            float fallbackCadenceMultiplier)
        {
            ResolveReferences();
            if (activeAura != auraKind)
            {
                ClearAuraEffects();
            }

            activeAura = auraKind;
            auraLingerRemaining = AuraLingerDuration;
            switch (auraKind)
            {
                case AuraKind.Speed:
                    ApplyMoveSpeedMultiplier(moveSpeedMultiplier);
                    break;
                case AuraKind.Bulwark:
                    if (health != null)
                    {
                        health.SetIncomingDamageMultiplier(damageTakenMultiplier);
                    }
                    break;
                case AuraKind.Mend:
                    healPerTick = Mathf.Max(0.01f, healingPerTick);
                    healTickInterval = Mathf.Max(0.1f, healingTickInterval);
                    healTimer = 0f;
                    break;
                case AuraKind.Volley:
                    if (projectileTargets.Count > 0)
                    {
                        ApplyProjectileMultiplier(projectileMultiplier);
                    }
                    else
                    {
                        ApplyCadenceMultiplier(fallbackCadenceMultiplier);
                    }
                    break;
            }
        }

        public void ClearAura()
        {
            ClearAuraEffects();
            activeAura = AuraKind.None;
            healPerTick = 0f;
            healTickInterval = 0f;
            healTimer = 0f;
            auraLingerRemaining = 0f;
            UpdateGlow();
        }

        private void ClearAuraEffects()
        {
            if (health != null)
            {
                health.SetIncomingDamageMultiplier(1f);
            }

            for (var i = 0; i < moveTargets.Count; i++)
            {
                moveTargets[i].SetAuraMoveSpeedMultiplier(1f);
            }

            for (var i = 0; i < cadenceTargets.Count; i++)
            {
                cadenceTargets[i].SetAuraCadenceMultiplier(1f);
            }

            for (var i = 0; i < projectileTargets.Count; i++)
            {
                projectileTargets[i].SetAuraProjectileMultiplier(1);
            }

            if (contactDamage != null)
            {
                contactDamage.SetAuraCadenceMultiplier(1f);
            }
        }

        private void UpdateGlow()
        {
            ResolveReferences();
            if (primaryRenderer == null)
            {
                return;
            }

            if (activeAura == AuraKind.None)
            {
                primaryRenderer.color = baseTint;
                if (glowRenderer != null)
                {
                    glowRenderer.enabled = false;
                }

                return;
            }

            var auraColor = ResolveAuraColor(activeAura);
            primaryRenderer.color = Color.Lerp(baseTint, auraColor, 0.18f);
            if (glowRenderer == null)
            {
                return;
            }

            glowRenderer.enabled = true;
            glowRenderer.sprite = primaryRenderer.sprite;
            glowRenderer.flipX = primaryRenderer.flipX;
            glowRenderer.flipY = primaryRenderer.flipY;
            glowRenderer.sortingOrder = primaryRenderer.sortingOrder + 1;
            var pulse = 0.2f + ((Mathf.Sin(Time.time * 3.4f) + 1f) * 0.5f * 0.24f);
            glowRenderer.color = new Color(auraColor.r, auraColor.g, auraColor.b, pulse);
        }

        private void ApplyMoveSpeedMultiplier(float multiplier)
        {
            var resolved = Mathf.Max(1f, multiplier);
            for (var i = 0; i < moveTargets.Count; i++)
            {
                moveTargets[i].SetAuraMoveSpeedMultiplier(resolved);
            }
        }

        private void ApplyCadenceMultiplier(float multiplier)
        {
            var resolved = Mathf.Max(1f, multiplier);
            for (var i = 0; i < cadenceTargets.Count; i++)
            {
                cadenceTargets[i].SetAuraCadenceMultiplier(resolved);
            }

            if (contactDamage != null)
            {
                contactDamage.SetAuraCadenceMultiplier(resolved);
            }
        }

        private void ApplyProjectileMultiplier(int multiplier)
        {
            var resolved = Mathf.Max(1, multiplier);
            for (var i = 0; i < projectileTargets.Count; i++)
            {
                projectileTargets[i].SetAuraProjectileMultiplier(resolved);
            }
        }

        private void ResolveReferences()
        {
            health ??= GetComponent<Health>();
            hurtbox ??= GetComponent<Hurtbox>();
            contactDamage ??= GetComponent<ContactDamage>();
            if (primaryRenderer == null)
            {
                primaryRenderer = GetComponentInChildren<SpriteRenderer>();
                if (primaryRenderer != null)
                {
                    baseTint = primaryRenderer.color;
                    var glowTransform = primaryRenderer.transform.Find("AuraGlow");
                    if (glowTransform == null)
                    {
                        var glowObject = new GameObject("AuraGlow");
                        glowObject.transform.SetParent(primaryRenderer.transform, false);
                        glowObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);
                        glowObject.transform.localScale = Vector3.one * 1.08f;
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

            moveTargets.Clear();
            cadenceTargets.Clear();
            projectileTargets.Clear();
            behaviourCache.Clear();
            GetComponents(behaviourCache);
            for (var i = 0; i < behaviourCache.Count; i++)
            {
                var behaviour = behaviourCache[i];
                if (behaviour is IAuraMoveSpeedTarget moveTarget)
                {
                    moveTargets.Add(moveTarget);
                }

                if (behaviour is IAuraCadenceTarget cadenceTarget)
                {
                    cadenceTargets.Add(cadenceTarget);
                }

                if (behaviour is IAuraProjectileTarget projectileTarget)
                {
                    projectileTargets.Add(projectileTarget);
                }
            }
        }

        private static Color ResolveAuraColor(AuraKind auraKind)
        {
            return auraKind switch
            {
                AuraKind.Speed => SpeedAuraColor,
                AuraKind.Bulwark => BulwarkAuraColor,
                AuraKind.Mend => MendAuraColor,
                AuraKind.Volley => VolleyAuraColor,
                _ => Color.white
            };
        }
    }

    public interface IAuraMoveSpeedTarget
    {
        void SetAuraMoveSpeedMultiplier(float multiplier);
    }

    public interface IAuraCadenceTarget
    {
        void SetAuraCadenceMultiplier(float multiplier);
    }

    public interface IAuraProjectileTarget
    {
        void SetAuraProjectileMultiplier(int multiplier);
    }
}
