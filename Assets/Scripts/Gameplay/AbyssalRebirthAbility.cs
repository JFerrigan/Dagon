using System.Collections.Generic;
using Dagon.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dagon.Gameplay
{
    public sealed class AbyssalRebirthAbility : ActiveAbilityRuntime
    {
        private static readonly Color SurgeTint = new(0.86f, 0.18f, 0.18f, 0.34f);
        private static readonly Color OutlineTint = new(1f, 0.86f, 0.86f, 0.82f);

        [SerializeField] private float cooldown = 11f;
        [SerializeField] private float radius = 5.8f;
        [SerializeField] private float damage = 6.5f;
        [SerializeField] private float immunityDuration = 0.6f;
        [SerializeField] private float overlayHeightOffset = 0.05f;
        [SerializeField] private float overlayThickness = 0.28f;
        [SerializeField] private float outlineThickness = 0.08f;
        [SerializeField] private LayerMask enemyMask = ~0;

        private readonly HashSet<GameObject> resolvedTargets = new();
        private TemporaryDamageImmunity damageImmunity;
        private Camera worldCamera;
        private float cooldownRemaining;

        public override float CooldownRemaining => cooldownRemaining;
        public override float CooldownDuration => cooldown;

        private void Awake()
        {
            damageImmunity = GetComponent<TemporaryDamageImmunity>();
            if (damageImmunity == null)
            {
                damageImmunity = gameObject.AddComponent<TemporaryDamageImmunity>();
            }
        }

        private void Update()
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.unscaledDeltaTime);
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TryActivate();
            }
        }

        public override void ConfigureRuntime(Camera cameraReference)
        {
            worldCamera = cameraReference;
        }

        public override void ModifyRadius(float amount)
        {
            radius = Mathf.Max(1.5f, radius + amount);
        }

        protected override void ApplyDefinition(ActiveAbilityDefinition runtimeDefinition)
        {
            cooldown = Mathf.Max(0.5f, runtimeDefinition.Cooldown);
            radius = Mathf.Max(1.5f, runtimeDefinition.Radius);
            damage = Mathf.Max(0.1f, runtimeDefinition.Damage);
            immunityDuration = Mathf.Max(0.1f, runtimeDefinition.Duration);
        }

        protected override void ApplyUpgrade(int nextRank)
        {
            switch (nextRank)
            {
                case 2:
                    radius += 0.8f;
                    break;
                case 3:
                    damage += 1.5f;
                    break;
                case 4:
                    cooldown = Mathf.Max(5f, cooldown - 1f);
                    break;
                case 5:
                    immunityDuration += 0.18f;
                    damage += 1f;
                    break;
                default:
                    radius += 0.25f;
                    damage += 0.4f;
                    break;
            }
        }

        protected override string GetUpgradeTitle(int nextRank)
        {
            return nextRank switch
            {
                2 => "Rebirth Reach",
                3 => "Rebirth Weight",
                4 => "Rebirth Recovery",
                5 => "Rebirth Shell",
                _ => $"Abyssal Rebirth +{nextRank - 1}"
            };
        }

        protected override string GetUpgradeDescription(int nextRank)
        {
            return nextRank switch
            {
                2 => "+0.8 Radius",
                3 => "+1.5 Damage",
                4 => "-1.0s Cooldown",
                5 => "+0.18s Immunity, +1.0 Damage",
                _ => "+0.25 Radius, +0.4 Damage"
            };
        }

        private void TryActivate()
        {
            if (cooldownRemaining > 0f)
            {
                return;
            }

            ResolveHits();
            damageImmunity?.Grant(immunityDuration);
            SpawnVisuals();
            cooldownRemaining = cooldown;
        }

        private void ResolveHits()
        {
            var colliders = Physics.OverlapSphere(transform.position, radius, enemyMask, QueryTriggerInteraction.Collide);
            resolvedTargets.Clear();
            for (var i = 0; i < colliders.Length; i++)
            {
                if (!CombatResolver.TryResolveUniqueHit(colliders[i], CombatTeam.Player, gameObject, resolvedTargets, out var resolvedHit))
                {
                    continue;
                }

                CombatResolver.TryApplyDamage(resolvedHit, gameObject, damage, CombatTeam.Player);
            }
        }

        private void SpawnVisuals()
        {
            RotLanternRadiusVisual.Spawn(transform.position, radius, overlayHeightOffset, overlayThickness, SurgeTint, 0.42f, 1.1f, 15);
            RotLanternRadiusVisual.Spawn(transform.position, radius, overlayHeightOffset + 0.01f, outlineThickness, OutlineTint, 0.42f, 1.12f, 16);
            PlaceholderWeaponVisual.Spawn(
                "AbyssalRebirthBurst",
                transform.position + Vector3.up * 0.06f,
                new Vector3(radius * 2.25f, radius * 2.25f, 1f),
                worldCamera != null ? worldCamera : Camera.main,
                new Color(0.82f, 0.12f, 0.12f, 0.42f),
                0.34f,
                1.16f,
                0f,
                spritePath: "Sprites/Effects/brine_surge",
                pixelsPerUnit: 256f,
                sortingOrder: 17,
                groundPlane: true);
        }
    }
}
