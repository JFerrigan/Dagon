using Dagon.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dagon.Gameplay
{
    public sealed class DashAbility : ActiveAbilityRuntime
    {
        [SerializeField] private float rechargeDuration = 4.5f;
        [SerializeField] private float dashDistance = 5.4f;
        [SerializeField] private float dashDuration = 0.18f;
        [SerializeField] private int maxCharges = 1;

        private PlayerMover playerMover;
        private TemporaryDamageImmunity damageImmunity;
        private int currentCharges;
        private float rechargeRemaining;

        public override float CooldownRemaining => currentCharges >= maxCharges ? 0f : rechargeRemaining;
        public override float CooldownDuration => ResolveCooldownDuration(rechargeDuration);

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
            if (currentCharges < maxCharges)
            {
                rechargeRemaining -= Time.unscaledDeltaTime;
                if (rechargeRemaining <= 0f)
                {
                    currentCharges += 1;
                    if (currentCharges < maxCharges)
                    {
                        rechargeRemaining = CooldownDuration;
                    }
                }
            }

            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TryActivate();
            }
        }

        public override void ConfigureRuntime(Camera worldCamera)
        {
        }

        public override void ModifyRadius(float amount)
        {
            dashDistance = Mathf.Max(2f, dashDistance + amount);
        }

        protected override void ApplyDefinition(ActiveAbilityDefinition runtimeDefinition)
        {
            rechargeDuration = Mathf.Max(0.5f, runtimeDefinition.Cooldown);
            dashDistance = Mathf.Max(2f, runtimeDefinition.Radius);
            dashDuration = Mathf.Max(0.08f, runtimeDefinition.Duration);
            maxCharges = Mathf.Max(1, runtimeDefinition.Charges);
            currentCharges = maxCharges;
            rechargeRemaining = 0f;
        }

        protected override void ApplyUpgrade(int nextRank)
        {
            switch (nextRank)
            {
                case 2:
                    maxCharges += 1;
                    currentCharges = maxCharges;
                    break;
                case 3:
                    rechargeDuration = Mathf.Max(1.5f, rechargeDuration - 0.75f);
                    break;
                case 4:
                    maxCharges += 1;
                    currentCharges = maxCharges;
                    break;
                case 5:
                    dashDistance += 1.25f;
                    break;
                default:
                    rechargeDuration = Mathf.Max(1f, rechargeDuration - 0.2f);
                    dashDistance += 0.25f;
                    currentCharges = Mathf.Min(currentCharges + 1, maxCharges);
                    break;
            }
        }

        protected override string GetUpgradeTitle(int nextRank)
        {
            return nextRank switch
            {
                2 => "Bilge Dash Charge I",
                3 => "Bilge Dash Recovery",
                4 => "Bilge Dash Charge II",
                5 => "Bilge Dash Reach",
                _ => $"Bilge Dash +{nextRank - 1}"
            };
        }

        protected override string GetUpgradeDescription(int nextRank)
        {
            return nextRank switch
            {
                2 => "+1 Dash Charge",
                3 => "-0.75s Recharge",
                4 => "+1 Dash Charge",
                5 => "+1.25 Dash Distance",
                _ => "-0.2s Recharge, +0.25 Distance"
            };
        }

        private void TryActivate()
        {
            if (currentCharges <= 0 || playerMover == null)
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

            currentCharges -= 1;
            if (currentCharges < maxCharges && rechargeRemaining <= 0f)
            {
                rechargeRemaining = CooldownDuration;
            }

            damageImmunity?.Grant(dashDuration * 1.05f);
            PlaceholderWeaponVisual.Spawn(
                "DashBurst",
                transform.position + Vector3.up * 0.05f,
                new Vector3(2.2f, 2.2f, 1f),
                Camera.main,
                new Color(0.78f, 0.92f, 0.84f, 0.35f),
                dashDuration,
                1.12f,
                groundPlane: true);
            NotifyActivated();
        }
    }
}
