using Dagon.Core;
using Dagon.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dagon.Gameplay
{
    public sealed class BrineSurgeAbility : ActiveAbilityRuntime
    {
        [SerializeField] private float cooldown = 6f;
        [SerializeField] private float radius = 2.8f;
        [SerializeField] private float damage = 2f;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private Camera worldCamera;

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
