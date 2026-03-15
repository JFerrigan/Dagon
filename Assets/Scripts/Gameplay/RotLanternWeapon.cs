using System.Collections;
using Dagon.Core;
using Dagon.Data;
using UnityEngine;

namespace Dagon.Gameplay
{
    public sealed class RotLanternWeapon : PlayerWeaponRuntime
    {
        private const string LanternSpritePath = "Sprites/Weapons/rot_lantern";
        private const string PulseSpritePath = "Sprites/Effects/brine_surge";

        [SerializeField] private float pulseRate = 0.75f;
        [SerializeField] private float damage = 0.8f;
        [SerializeField] private float radius = 2.2f;
        [SerializeField] private int pulseCount = 1;
        [SerializeField] private LayerMask enemyMask = ~0;

        private Camera worldCamera;
        private float cooldownTimer = 0.2f;

        public override string PathAName => "Lantern Choir";
        public override string PathBName => "Baleful Flame";

        private void Update()
        {
            if (pulseRate <= 0f)
            {
                return;
            }

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer > 0f)
            {
                return;
            }

            StartCoroutine(PulseSequence());
            cooldownTimer = 1f / pulseRate;
        }

        public override void ConfigureRuntime(Camera worldCameraReference)
        {
            worldCamera = worldCameraReference;
        }

        public override void ModifyAttackRate(float amount)
        {
            pulseRate = Mathf.Max(0.1f, pulseRate + amount);
        }

        public override void ModifyProjectileDamage(float amount)
        {
            damage = Mathf.Max(0.1f, damage + amount);
        }

        public override void ModifyProjectileCount(int amount)
        {
            pulseCount = Mathf.Max(1, pulseCount + amount);
        }

        protected override void ApplyDefinition(WeaponDefinition runtimeDefinition)
        {
            pulseRate = Mathf.Max(0.1f, runtimeDefinition.AttacksPerSecond);
            damage = Mathf.Max(0.1f, runtimeDefinition.ProjectileDamage);
            radius = Mathf.Max(0.5f, runtimeDefinition.EffectRadius);
            pulseCount = Mathf.Max(1, runtimeDefinition.ProjectilesPerVolley);
        }

        protected override void ApplyPathUpgrade(WeaponUpgradePath path, int nextStep)
        {
            switch (path)
            {
                case WeaponUpgradePath.PathA:
                    if (nextStep <= 2)
                    {
                        pulseCount = nextStep + 1;
                    }
                    else
                    {
                        damage = 1.1f;
                    }
                    break;
                case WeaponUpgradePath.PathB:
                    damage = nextStep switch
                    {
                        1 => 1.2f,
                        2 => 1.6f,
                        _ => 2.0f
                    };
                    break;
            }
        }

        protected override string GetUpgradeTitle(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "Lantern Choir I",
                WeaponUpgradePath.PathA when nextStep == 2 => "Lantern Choir II",
                WeaponUpgradePath.PathA => "Lantern Choir III",
                WeaponUpgradePath.PathB when nextStep == 1 => "Baleful Wick I",
                WeaponUpgradePath.PathB when nextStep == 2 => "Baleful Wick II",
                _ => "Baleful Wick III"
            };
        }

        protected override string GetUpgradeDescription(WeaponUpgradePath path, int nextStep)
        {
            return path switch
            {
                WeaponUpgradePath.PathA when nextStep == 1 => "Each lantern trigger emits two pulses.",
                WeaponUpgradePath.PathA when nextStep == 2 => "Each lantern trigger emits three pulses.",
                WeaponUpgradePath.PathA => "Increase pulse damage to 1.1.",
                WeaponUpgradePath.PathB when nextStep == 1 => "Increase pulse damage to 1.2.",
                WeaponUpgradePath.PathB when nextStep == 2 => "Increase pulse damage to 1.6.",
                _ => "Increase pulse damage to 2.0."
            };
        }

        private IEnumerator PulseSequence()
        {
            for (var i = 0; i < pulseCount; i++)
            {
                Pulse();
                if (pulseCount > 1 && i < pulseCount - 1)
                {
                    yield return new WaitForSeconds(0.12f);
                }
            }
        }

        private void Pulse()
        {
            PlaceholderWeaponVisual.Spawn(
                "RotLanternArea",
                transform.position + Vector3.up * 0.05f,
                new Vector3(radius * 1.35f, radius * 1.35f, 1f),
                worldCamera,
                new Color(0.56f, 0.92f, 0.62f, 0.32f),
                0.32f,
                1.04f,
                0f,
                spritePath: PulseSpritePath,
                pixelsPerUnit: 256f,
                sortingOrder: 4,
                groundPlane: true);

            var colliders = Physics.OverlapSphere(transform.position, radius, enemyMask, QueryTriggerInteraction.Collide);
            foreach (var hit in colliders)
            {
                if (hit.transform == transform)
                {
                    continue;
                }

                CombatResolver.TryApplyDamage(hit, CombatTeam.Player, gameObject, damage);
            }

            PlaceholderWeaponVisual.Spawn(
                "RotLanternPulse",
                transform.position + Vector3.up * 0.18f,
                new Vector3(radius * 0.55f, radius * 0.55f, 1f),
                worldCamera,
                new Color(0.72f, 0.96f, 0.72f, 0.5f),
                0.24f,
                1.18f,
                spritePath: LanternSpritePath,
                pixelsPerUnit: 256f);
        }
    }
}
