using Dagon.Core;
using Dagon.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dagon.Gameplay
{
    public sealed class BrineSurgeAbility : ActiveAbilityRuntime
    {
        private static readonly Color SurgeOverlayTint = new(0.34f, 0.94f, 0.66f, 0.26f);
        private static readonly Color SurgeOutlineTint = new(0.86f, 1f, 0.90f, 0.72f);

        [SerializeField] private float cooldown = 6f;
        [SerializeField] private float radius = 4.2f;
        [SerializeField] private float damage = 5f;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private bool showRadiusOverlay = true;
        [SerializeField] private float radiusOverlayHeightOffset = 0.05f;
        [SerializeField] private float radiusOverlayThickness = 0.24f;
        [SerializeField] private float radiusOutlineThickness = 0.08f;
        [SerializeField] private float radiusOverlayDuration = 0.4f;
        [SerializeField] private float radiusOverlayEndScaleMultiplier = 1.08f;
        [SerializeField] private int radiusOverlaySortingOrder = 14;

        private float cooldownRemaining;
        private readonly HashSet<GameObject> resolvedTargets = new();

        public override float CooldownRemaining => cooldownRemaining;
        public override float CooldownDuration => cooldown;

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
            radius = Mathf.Max(1f, radius + amount);
        }

        protected override void ApplyDefinition(ActiveAbilityDefinition runtimeDefinition)
        {
            cooldown = Mathf.Max(0.25f, runtimeDefinition.Cooldown);
            radius = Mathf.Max(1f, runtimeDefinition.Radius);
            damage = Mathf.Max(0.1f, runtimeDefinition.Damage);
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
                    cooldown = Mathf.Max(1.8f, cooldown - 1f);
                    break;
                case 5:
                    radius += 1f;
                    damage += 1f;
                    break;
                default:
                    radius += 0.3f;
                    damage += 0.35f;
                    break;
            }
        }

        protected override string GetUpgradeTitle(int nextRank)
        {
            return nextRank switch
            {
                2 => "Brine Reach",
                3 => "Undertow Weight",
                4 => "Brine Recovery",
                5 => "Undertow Bloom",
                _ => $"Brine Surge +{nextRank - 1}"
            };
        }

        protected override string GetUpgradeDescription(int nextRank)
        {
            return nextRank switch
            {
                2 => "+0.8 Radius",
                3 => "+1.5 Damage",
                4 => "-1.0s Cooldown",
                5 => "+1.0 Radius, +1.0 Damage",
                _ => "+0.3 Radius, +0.35 Damage"
            };
        }

        private void TryActivate()
        {
            if (cooldownRemaining > 0f)
            {
                return;
            }

            var colliders = Physics.OverlapSphere(transform.position, radius, enemyMask, QueryTriggerInteraction.Collide);
            resolvedTargets.Clear();
            foreach (var hit in colliders)
            {
                if (hit.transform == transform)
                {
                    continue;
                }

                if (!CombatResolver.TryResolveUniqueHit(hit, CombatTeam.Player, gameObject, resolvedTargets, out var resolvedHit))
                {
                    continue;
                }

                CombatResolver.TryApplyDamage(resolvedHit, gameObject, damage, CombatTeam.Player);
            }

            if (showRadiusOverlay)
            {
                RotLanternRadiusVisual.Spawn(
                    transform.position,
                    radius,
                    radiusOverlayHeightOffset,
                    radiusOverlayThickness,
                    SurgeOverlayTint,
                    radiusOverlayDuration,
                    radiusOverlayEndScaleMultiplier,
                    radiusOverlaySortingOrder);

                RotLanternRadiusVisual.Spawn(
                    transform.position,
                    radius,
                    radiusOverlayHeightOffset + 0.01f,
                    radiusOutlineThickness,
                    SurgeOutlineTint,
                    radiusOverlayDuration,
                    radiusOverlayEndScaleMultiplier,
                    radiusOverlaySortingOrder + 1);
            }

            BrineSurgeVisual.Spawn(transform.position, radius, ResolveCamera());
            cooldownRemaining = cooldown;
        }

        private Camera ResolveCamera()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            return worldCamera;
        }
    }
}
