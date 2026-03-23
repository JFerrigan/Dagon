using System.Collections.Generic;
using Dagon.Core;
using Dagon.Data;
using Dagon.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dagon.Gameplay
{
    public sealed class RiftheartAbility : ActiveAbilityRuntime
    {
        [SerializeField] private float cooldown = 15f;
        [SerializeField] private float duration = 4.5f;
        [SerializeField] private float attackSpeedMultiplier = 2.15f;
        [SerializeField] private int shardCount = 4;
        [SerializeField] private float orbitRadius = 1.9f;
        [SerializeField] private float orbitSpeedDegrees = 150f;
        [SerializeField] private float shardPulseInterval = 0.18f;
        [SerializeField] private float shardHitRadius = 0.7f;
        [SerializeField] private float shardDamage = 1f;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private string shardSpritePath = "Sprites/Enemies/watcher_eye";
        [SerializeField] private float shardPixelsPerUnit = 256f;
        [SerializeField] private Color shardTint = new(0.90f, 0.18f, 0.22f, 0.95f);
        [SerializeField] private Vector3 shardScale = new(0.55f, 0.55f, 1f);

        private readonly Dictionary<PlayerWeaponRuntime, float> appliedBonuses = new();
        private readonly List<Transform> shardVisuals = new();
        private readonly HashSet<GameObject> resolvedTargets = new();
        private PlayerCombatLoadout combatLoadout;
        private Camera worldCamera;
        private float cooldownRemaining;
        private float activeRemaining;
        private float shardPulseTimer;

        public override float CooldownRemaining => cooldownRemaining;
        public override float CooldownDuration => cooldown;

        private void Awake()
        {
            combatLoadout = GetComponent<PlayerCombatLoadout>();
        }

        private void Update()
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.unscaledDeltaTime);
            if (activeRemaining > 0f)
            {
                activeRemaining = Mathf.Max(0f, activeRemaining - Time.unscaledDeltaTime);
                UpdateShards();
                if (activeRemaining <= 0f)
                {
                    RemoveBuff();
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

        private void OnDisable()
        {
            RemoveBuff();
        }

        public override void ConfigureRuntime(Camera cameraReference)
        {
            worldCamera = cameraReference;
        }

        public override void ModifyRadius(float amount)
        {
            orbitRadius = Mathf.Max(0.8f, orbitRadius + (amount * 0.35f));
            shardHitRadius = Mathf.Max(0.25f, shardHitRadius + (amount * 0.12f));
            duration = Mathf.Max(0.5f, duration + (amount * 0.12f));
        }

        protected override void ApplyDefinition(ActiveAbilityDefinition runtimeDefinition)
        {
            cooldown = Mathf.Max(0.5f, runtimeDefinition.Cooldown);
            duration = Mathf.Max(0.5f, runtimeDefinition.Duration);
            attackSpeedMultiplier = Mathf.Max(1.1f, runtimeDefinition.Magnitude);
            shardDamage = Mathf.Max(0.1f, runtimeDefinition.Damage);
            orbitRadius = Mathf.Max(0.8f, runtimeDefinition.Radius);
        }

        protected override void ApplyUpgrade(int nextRank)
        {
            switch (nextRank)
            {
                case 2:
                    shardCount += 1;
                    break;
                case 3:
                    attackSpeedMultiplier += 0.25f;
                    break;
                case 4:
                    duration += 0.8f;
                    break;
                case 5:
                    shardDamage += 0.45f;
                    break;
                default:
                    attackSpeedMultiplier += 0.08f;
                    shardDamage += 0.15f;
                    break;
            }
        }

        protected override string GetUpgradeTitle(int nextRank)
        {
            return nextRank switch
            {
                2 => "Riftheart Halo",
                3 => "Riftheart Tempo",
                4 => "Riftheart Hold",
                5 => "Riftheart Bite",
                _ => $"Riftheart +{nextRank - 1}"
            };
        }

        protected override string GetUpgradeDescription(int nextRank)
        {
            return nextRank switch
            {
                2 => "+1 Shard",
                3 => "+0.25x Attack Speed",
                4 => "+0.8s Duration",
                5 => "+0.45 Shard Damage",
                _ => "+0.08x Attack Speed, +0.15 Shard Damage"
            };
        }

        private void TryActivate()
        {
            if (cooldownRemaining > 0f || combatLoadout == null)
            {
                return;
            }

            ApplyBuff();
            SpawnShards();
            activeRemaining = duration;
            shardPulseTimer = 0f;
            cooldownRemaining = cooldown;

            PlaceholderWeaponVisual.Spawn(
                "RiftheartBurst",
                transform.position + Vector3.up * 0.08f,
                new Vector3(3.8f, 3.8f, 1f),
                worldCamera != null ? worldCamera : Camera.main,
                new Color(0.92f, 0.16f, 0.22f, 0.34f),
                0.32f,
                1.12f,
                0f,
                spritePath: "Sprites/Effects/brine_surge",
                pixelsPerUnit: 256f,
                sortingOrder: 16,
                groundPlane: true);
        }

        private void ApplyBuff()
        {
            RemoveBuff();
            for (var i = 0; i < combatLoadout.Weapons.Count; i++)
            {
                var weapon = combatLoadout.Weapons[i];
                if (weapon == null)
                {
                    continue;
                }

                var currentRate = Mathf.Max(0.1f, weapon.GetAttackRateEstimate());
                var bonus = currentRate * (attackSpeedMultiplier - 1f);
                if (bonus <= 0f)
                {
                    continue;
                }

                weapon.ModifyAttackRate(bonus);
                appliedBonuses[weapon] = bonus;
            }
        }

        private void UpdateShards()
        {
            if (shardVisuals.Count <= 0)
            {
                return;
            }

            var angleStep = 360f / Mathf.Max(1, shardVisuals.Count);
            var baseAngle = Time.time * orbitSpeedDegrees;
            for (var i = 0; i < shardVisuals.Count; i++)
            {
                var shard = shardVisuals[i];
                if (shard == null)
                {
                    continue;
                }

                var angle = (baseAngle + (angleStep * i)) * Mathf.Deg2Rad;
                var localOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitRadius;
                shard.position = transform.position + Vector3.up * 0.4f + localOffset;
            }

            shardPulseTimer -= Time.deltaTime;
            if (shardPulseTimer > 0f)
            {
                return;
            }

            shardPulseTimer = shardPulseInterval;
            for (var i = 0; i < shardVisuals.Count; i++)
            {
                var shard = shardVisuals[i];
                if (shard == null)
                {
                    continue;
                }

                resolvedTargets.Clear();
                var colliders = Physics.OverlapSphere(shard.position, shardHitRadius, enemyMask, QueryTriggerInteraction.Collide);
                for (var hitIndex = 0; hitIndex < colliders.Length; hitIndex++)
                {
                    if (!CombatResolver.TryResolveUniqueHit(colliders[hitIndex], CombatTeam.Player, gameObject, resolvedTargets, out var resolvedHit))
                    {
                        continue;
                    }

                    CombatResolver.TryApplyDamage(resolvedHit, gameObject, shardDamage, CombatTeam.Player);
                }

                PlaceholderWeaponVisual.Spawn(
                    "RiftheartShardPulse",
                    shard.position,
                    new Vector3(shardHitRadius * 1.5f, shardHitRadius * 1.5f, 1f),
                    worldCamera != null ? worldCamera : Camera.main,
                    new Color(0.94f, 0.24f, 0.28f, 0.24f),
                    0.12f,
                    1.05f,
                    0f,
                    spritePath: "Sprites/Effects/brine_surge",
                    pixelsPerUnit: 256f,
                    sortingOrder: 15,
                    groundPlane: false);
            }
        }

        private void SpawnShards()
        {
            ClearShards();
            var sprite = RuntimeSpriteLibrary.LoadSprite(shardSpritePath, shardPixelsPerUnit);
            if (sprite == null)
            {
                return;
            }

            for (var i = 0; i < Mathf.Max(1, shardCount); i++)
            {
                var shard = new GameObject($"RiftheartShard_{i + 1}");
                shard.transform.SetParent(transform, true);

                var renderer = shard.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = shardTint;
                renderer.sortingOrder = 14;
                shard.transform.localScale = shardScale;

                var billboard = shard.AddComponent<BillboardSprite>();
                billboard.Configure(worldCamera != null ? worldCamera : Camera.main, BillboardSprite.BillboardMode.YAxisOnly);
                shardVisuals.Add(shard.transform);
            }
        }

        private void RemoveBuff()
        {
            foreach (var pair in appliedBonuses)
            {
                if (pair.Key != null)
                {
                    pair.Key.ModifyAttackRate(-pair.Value);
                }
            }

            appliedBonuses.Clear();
            activeRemaining = 0f;
            ClearShards();
        }

        private void ClearShards()
        {
            for (var i = 0; i < shardVisuals.Count; i++)
            {
                if (shardVisuals[i] != null)
                {
                    Destroy(shardVisuals[i].gameObject);
                }
            }

            shardVisuals.Clear();
        }
    }
}
