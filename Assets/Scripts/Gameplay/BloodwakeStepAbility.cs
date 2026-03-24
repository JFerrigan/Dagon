using System.Collections;
using System.Collections.Generic;
using Dagon.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dagon.Gameplay
{
    public sealed class BloodwakeStepAbility : ActiveAbilityRuntime
    {
        private static readonly Color StartTint = new(0.70f, 0.12f, 0.16f, 0.30f);
        private static readonly Color EndTint = new(0.92f, 0.18f, 0.22f, 0.40f);

        [SerializeField] private float cooldown = 8f;
        [SerializeField] private float dashDistance = 6.6f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float startBurstRadius = 2.6f;
        [SerializeField] private float startBurstDamage = 2.5f;
        [SerializeField] private float endBurstRadius = 4.1f;
        [SerializeField] private float endBurstDamage = 5f;
        [SerializeField] private LayerMask enemyMask = ~0;

        private readonly HashSet<GameObject> resolvedTargets = new();
        private PlayerMover playerMover;
        private TemporaryDamageImmunity damageImmunity;
        private Camera worldCamera;
        private float cooldownRemaining;
        private bool activationLocked;

        public override float CooldownRemaining => cooldownRemaining;
        public override float CooldownDuration => ResolveCooldownDuration(cooldown);

        private void Awake()
        {
            playerMover = GetComponent<PlayerMover>();
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
            endBurstRadius = Mathf.Max(1.5f, endBurstRadius + amount);
            startBurstRadius = Mathf.Max(1f, startBurstRadius + (amount * 0.45f));
            dashDistance = Mathf.Max(2f, dashDistance + (amount * 0.25f));
        }

        protected override void ApplyDefinition(ActiveAbilityDefinition runtimeDefinition)
        {
            cooldown = Mathf.Max(0.5f, runtimeDefinition.Cooldown);
            dashDistance = Mathf.Max(2f, runtimeDefinition.Radius);
            dashDuration = Mathf.Max(0.08f, runtimeDefinition.Duration);
            endBurstDamage = Mathf.Max(0.1f, runtimeDefinition.Damage);
            startBurstDamage = Mathf.Max(0.1f, endBurstDamage * 0.5f);
            startBurstRadius = 2.6f;
            endBurstRadius = 4.1f;
        }

        protected override void ApplyUpgrade(int nextRank)
        {
            switch (nextRank)
            {
                case 2:
                    dashDistance += 1f;
                    break;
                case 3:
                    endBurstDamage += 1.5f;
                    startBurstDamage += 0.8f;
                    break;
                case 4:
                    cooldown = Mathf.Max(4.5f, cooldown - 0.9f);
                    break;
                case 5:
                    endBurstRadius += 0.8f;
                    break;
                default:
                    endBurstDamage += 0.4f;
                    endBurstRadius += 0.2f;
                    break;
            }
        }

        protected override string GetUpgradeTitle(int nextRank)
        {
            return nextRank switch
            {
                2 => "Bloodwake Reach",
                3 => "Bloodwake Payload",
                4 => "Bloodwake Recovery",
                5 => "Bloodwake Bloom",
                _ => $"Bloodwake Step +{nextRank - 1}"
            };
        }

        protected override string GetUpgradeDescription(int nextRank)
        {
            return nextRank switch
            {
                2 => "+1.0 Dash Distance",
                3 => "+1.5 End Damage, +0.8 Start Damage",
                4 => "-0.9s Cooldown",
                5 => "+0.8 End Blast Radius",
                _ => "+0.4 Damage, +0.2 Radius"
            };
        }

        private void TryActivate()
        {
            if (cooldownRemaining > 0f || playerMover == null || activationLocked)
            {
                return;
            }

            var dashDirection = playerMover.MoveDirection.sqrMagnitude > 0.01f
                ? playerMover.MoveDirection.normalized
                : playerMover.AimDirection;
            if (dashDirection.sqrMagnitude <= 0.001f)
            {
                dashDirection = transform.forward;
            }

            if (!playerMover.StartDash(dashDirection, dashDistance, dashDuration))
            {
                return;
            }

            activationLocked = true;
            damageImmunity?.Grant(dashDuration * 1.1f);
            ResolveBurst(transform.position, startBurstRadius, startBurstDamage);
            SpawnBurstVisual(transform.position, startBurstRadius, StartTint, 15);
            cooldownRemaining = CooldownDuration;
            NotifyActivated();
            StartCoroutine(FinishDashBurst());
        }

        private IEnumerator FinishDashBurst()
        {
            yield return new WaitForSeconds(dashDuration);
            ResolveBurst(transform.position, endBurstRadius, endBurstDamage);
            SpawnBurstVisual(transform.position, endBurstRadius, EndTint, 16);
            activationLocked = false;
        }

        private void ResolveBurst(Vector3 origin, float radius, float damage)
        {
            var colliders = Physics.OverlapSphere(origin, radius, enemyMask, QueryTriggerInteraction.Collide);
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

        private void SpawnBurstVisual(Vector3 position, float radius, Color tint, int sortingOrder)
        {
            RotLanternRadiusVisual.Spawn(position, radius, 0.05f, 0.18f, tint, 0.28f, 1.08f, sortingOrder);
            PlaceholderWeaponVisual.Spawn(
                "BloodwakeBurst",
                position + Vector3.up * 0.05f,
                new Vector3(radius * 2.1f, radius * 2.1f, 1f),
                worldCamera != null ? worldCamera : Camera.main,
                tint,
                0.26f,
                1.14f,
                0f,
                spritePath: "Sprites/Effects/brine_surge",
                pixelsPerUnit: 256f,
                sortingOrder: sortingOrder + 1,
                groundPlane: true);
        }
    }
}
