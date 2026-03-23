using System.Collections.Generic;
using Dagon.Core;
using Dagon.Rendering;
using Dagon.UI;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DrownedAdmiralController : MonoBehaviour
    {
        private const float SummonHurtboxHeightLeniencyMultiplier = 1.3f;

        private const string ParasiteSpritePath = "Sprites/Enemies/parasite";

        private enum State
        {
            Pursuit,
            DashWindup,
            Dash,
            SlashWindup,
            LanternWindup,
            SummonWindup,
            Recover
        }

        [SerializeField] private Transform target;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private HarpoonProjectile lanternProjectilePrefab;
        [SerializeField] private float moveSpeed = 3.6f;
        [SerializeField] private float dashSpeed = 11f;
        [SerializeField] private float dashRange = 14f;
        [SerializeField] private float dashTravelDistance = 12.5f;
        [SerializeField] private float slashRange = 3.6f;
        [SerializeField] private float lanternRange = 9f;
        [SerializeField] private float slashAngle = 112f;

        private readonly HashSet<GameObject> resolvedTargets = new();
        private Sprite parasiteSprite;
        private State state;
        private float stateTimer;
        private float dashCooldownTimer;
        private float slashCooldownTimer;
        private float lanternCooldownTimer;
        private float summonCooldownTimer;
        private Vector3 dashDirection = Vector3.forward;
        private bool slashAfterDash;
        private int difficultyTier;
        private float damageMultiplier = 1f;
        private float cadenceMultiplier = 1f;

        private void Awake()
        {
            parasiteSprite = RuntimeSpriteLibrary.LoadSprite(ParasiteSpritePath, 64f);
        }

        private void Update()
        {
            ResolveReferences();
            if (target == null)
            {
                return;
            }

            dashCooldownTimer -= Time.deltaTime;
            slashCooldownTimer -= Time.deltaTime;
            lanternCooldownTimer -= Time.deltaTime;
            summonCooldownTimer -= Time.deltaTime;
            stateTimer -= Time.deltaTime;

            var toTarget = target.position - transform.position;
            toTarget.y = 0f;
            var distance = toTarget.magnitude;

            switch (state)
            {
                case State.Pursuit:
                    Pursue(toTarget);
                    TryStartAction(toTarget, distance);
                    break;
                case State.DashWindup:
                    if (stateTimer <= 0f)
                    {
                        state = State.Dash;
                        stateTimer = AdjustCooldown(Mathf.Max(0.2f, dashTravelDistance / Mathf.Max(0.1f, dashSpeed)));
                    }
                    break;
                case State.Dash:
                    transform.position += dashDirection * (dashSpeed * Time.deltaTime);
                    if (stateTimer <= 0f)
                    {
                        if (slashAfterDash)
                        {
                            BeginSlash(ResolveDirectionToTarget(), 0.18f, 1.8f);
                        }
                        else
                        {
                            EnterRecover(0.45f);
                        }
                    }
                    break;
                case State.SlashWindup:
                    if (stateTimer <= 0f)
                    {
                        PerformSlash();
                        EnterRecover(0.38f);
                    }
                    break;
                case State.LanternWindup:
                    if (stateTimer <= 0f)
                    {
                        FireLanternShot();
                        EnterRecover(0.34f);
                    }
                    break;
                case State.SummonWindup:
                    if (stateTimer <= 0f)
                    {
                        SummonParasitePack();
                        EnterRecover(0.5f);
                    }
                    break;
                case State.Recover:
                    if (stateTimer <= 0f)
                    {
                        state = State.Pursuit;
                    }
                    break;
            }
        }

        public void Configure(Transform newTarget, Camera cameraReference, HarpoonProjectile projectilePrefab, int newDifficultyTier)
        {
            target = newTarget;
            worldCamera = cameraReference;
            lanternProjectilePrefab = projectilePrefab;
            difficultyTier = Mathf.Max(0, newDifficultyTier);
            moveSpeed = 3.6f + (difficultyTier * 0.12f);
            dashCooldownTimer = 1.7f;
            slashCooldownTimer = 0.8f;
            lanternCooldownTimer = 1.2f;
            summonCooldownTimer = Mathf.Max(4f, 10f - (difficultyTier * 0.65f));
            state = State.Pursuit;
            stateTimer = 0f;
        }

        public void ApplyCorruptionModifiers(float newDamageMultiplier, float speedMultiplier, float newCadenceMultiplier)
        {
            damageMultiplier = Mathf.Max(0.1f, newDamageMultiplier);
            cadenceMultiplier = Mathf.Max(0.1f, newCadenceMultiplier);
            moveSpeed *= Mathf.Max(0.1f, speedMultiplier);
            dashSpeed *= Mathf.Max(0.1f, speedMultiplier);
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

            if (lanternProjectilePrefab == null && worldCamera != null)
            {
                lanternProjectilePrefab = RuntimeOrbProjectileFactory.Create(worldCamera);
            }
        }

        private void Pursue(Vector3 toTarget)
        {
            if (toTarget.sqrMagnitude <= 0.04f)
            {
                return;
            }

            transform.position += toTarget.normalized * (moveSpeed * Time.deltaTime);
        }

        private void TryStartAction(Vector3 toTarget, float distance)
        {
            if (distance <= slashRange && slashCooldownTimer <= 0f)
            {
                BeginSlash(toTarget.normalized, 0.22f, 1.5f);
                return;
            }

            if (distance <= lanternRange && distance > slashRange && lanternCooldownTimer <= 0f)
            {
                BeginLanternShot(toTarget.normalized);
                return;
            }

            if (distance <= dashRange && distance > 2.5f && dashCooldownTimer <= 0f)
            {
                BeginDash(toTarget.normalized, distance <= slashRange + 1.2f);
                return;
            }

            if (summonCooldownTimer <= 0f)
            {
                BeginParasiteCall();
            }
        }

        private void BeginDash(Vector3 direction, bool followWithSlash)
        {
            dashDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
            slashAfterDash = followWithSlash;
            dashCooldownTimer = AdjustCooldown(Mathf.Max(1.5f, 5.4f - (difficultyTier * 0.2f)));
            state = State.DashWindup;
            stateTimer = AdjustCooldown(0.42f);

            PlaceholderWeaponVisual.Spawn(
                "AdmiralDashTelegraph",
                transform.position + (dashDirection * (dashTravelDistance * 0.5f)) + Vector3.up * 0.05f,
                new Vector3(1.35f, dashTravelDistance, 1f),
                worldCamera,
                new Color(0.82f, 0.96f, 0.88f, 0.48f),
                stateTimer,
                1.02f,
                Mathf.Atan2(dashDirection.x, dashDirection.z) * Mathf.Rad2Deg,
                groundPlane: true);
        }

        private void BeginSlash(Vector3 direction, float windup, float damage)
        {
            dashDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
            slashCooldownTimer = AdjustCooldown(Mathf.Max(0.65f, 2.3f - (difficultyTier * 0.08f)));
            state = State.SlashWindup;
            stateTimer = AdjustCooldown(windup);
            SpawnSlashTelegraph(windup);

            pendingSlashDamage = damage * damageMultiplier;
        }

        private float pendingSlashDamage;

        private void PerformSlash()
        {
            var colliders = Physics.OverlapSphere(transform.position, slashRange, ~0, QueryTriggerInteraction.Collide);
            resolvedTargets.Clear();
            for (var i = 0; i < colliders.Length; i++)
            {
                var hit = colliders[i];
                var toHit = hit.transform.position - transform.position;
                toHit.y = 0f;
                if (toHit.sqrMagnitude <= 0.01f)
                {
                    continue;
                }

                if (Vector3.Angle(dashDirection, toHit.normalized) > slashAngle * 0.5f)
                {
                    continue;
                }

                if (!CombatResolver.TryResolveUniqueHit(hit, CombatTeam.Enemy, gameObject, resolvedTargets, out var resolvedHit))
                {
                    continue;
                }

                CombatResolver.TryApplyDamage(resolvedHit, gameObject, pendingSlashDamage, CombatTeam.Enemy);
            }
        }

        private void BeginLanternShot(Vector3 direction)
        {
            dashDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
            lanternCooldownTimer = AdjustCooldown(Mathf.Max(0.8f, 3.1f - (difficultyTier * 0.12f)));
            state = State.LanternWindup;
            stateTimer = AdjustCooldown(0.36f);

            PlaceholderWeaponVisual.Spawn(
                "AdmiralLanternTelegraph",
                transform.position + (dashDirection * 1.6f) + Vector3.up * 0.05f,
                new Vector3(0.9f, 3f, 1f),
                worldCamera,
                new Color(0.76f, 0.94f, 0.86f, 0.36f),
                stateTimer,
                1.03f,
                Mathf.Atan2(dashDirection.x, dashDirection.z) * Mathf.Rad2Deg,
                groundPlane: true);
        }

        private void FireLanternShot()
        {
            if (lanternProjectilePrefab == null)
            {
                return;
            }

            var projectile = Instantiate(
                lanternProjectilePrefab,
                transform.position + Vector3.up * 0.48f,
                Quaternion.LookRotation(dashDirection, Vector3.up));
            projectile.gameObject.SetActive(true);
            projectile.Initialize(gameObject, dashDirection, 7.8f + (difficultyTier * 0.18f), 1.4f * damageMultiplier);
        }

        private void BeginParasiteCall()
        {
            summonCooldownTimer = AdjustCooldown(Mathf.Max(4f, 10f - (difficultyTier * 0.65f)));
            state = State.SummonWindup;
            stateTimer = AdjustCooldown(0.5f);

            PlaceholderWeaponVisual.Spawn(
                "AdmiralSummonTelegraph",
                transform.position + Vector3.up * 0.06f,
                new Vector3(4.5f, 4.5f, 1f),
                worldCamera,
                new Color(0.74f, 0.94f, 0.80f, 0.28f),
                stateTimer,
                1.05f,
                groundPlane: true);
        }

        private void SummonParasitePack()
        {
            if (parasiteSprite == null)
            {
                return;
            }

            var packSize = Mathf.Clamp(2 + Mathf.FloorToInt(difficultyTier * 0.25f), 2, 4);
            for (var i = 0; i < packSize; i++)
            {
                var angle = i * (360f / packSize);
                var offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 2.2f;
                var spawnPosition = target != null ? target.position + new Vector3(offset.x, 0f, offset.z) : transform.position + offset;
                CreateParasite(spawnPosition);
            }
        }

        private void CreateParasite(Vector3 position)
        {
            var parasite = new GameObject("AdmiralParasite");
            parasite.transform.position = position;
            parasite.transform.SetParent(transform.parent, true);

            var collider = parasite.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(0f, 0.45f, 0f);
            collider.height = 0.9f;
            collider.radius = 0.38f;
            ApplyVerticalHurtboxLeniency(collider);
            parasite.AddComponent<BodyBlocker>().Configure(BodyBlocker.BodyTeam.Enemy, 0.28f, 0.9f, 0.7f, true, true);

            var rigidbody = parasite.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            var health = parasite.AddComponent<Health>();
            health.SetMaxHealth(1.3f, true);
            parasite.AddComponent<Hurtbox>().Configure(CombatTeam.Enemy, health);
            parasite.AddComponent<KnockbackReceiver>().Configure(1.2f, 16f, 5.8f);
            parasite.AddComponent<ContactDamage>().Configure(1f * damageMultiplier);
            parasite.AddComponent<ParasiteChaser>().Configure(target, (7.4f + (difficultyTier * 0.08f)) * 1.08f, 0.2f);
            parasite.AddComponent<EnemyDeathRewards>().Configure(0, 0f);

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(parasite.transform, false);
            visuals.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            visuals.transform.localScale = new Vector3(1.25f, 1.25f, 1f);

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = parasiteSprite;
            renderer.sortingOrder = 12;
            renderer.color = Color.white;

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);

            var healthBar = parasite.AddComponent<EnemyHealthBar>();
            healthBar.Configure(worldCamera, new Vector3(0f, 1.1f, 0f), true, 2.25f);
        }

        private static void ApplyVerticalHurtboxLeniency(CapsuleCollider collider)
        {
            var originalHeight = Mathf.Max(collider.radius * 2f, collider.height);
            var expandedHeight = Mathf.Max(originalHeight, originalHeight * SummonHurtboxHeightLeniencyMultiplier);
            var extraHeight = expandedHeight - originalHeight;
            collider.height = expandedHeight;
            collider.center += new Vector3(0f, extraHeight * 0.5f, 0f);
        }

        private void EnterRecover(float duration)
        {
            state = State.Recover;
            stateTimer = AdjustCooldown(duration);
        }

        private float AdjustCooldown(float baseCooldown)
        {
            return Mathf.Max(0.08f, baseCooldown / cadenceMultiplier);
        }

        private Vector3 ResolveDirectionToTarget()
        {
            if (target == null)
            {
                return dashDirection.sqrMagnitude > 0.001f ? dashDirection : transform.forward;
            }

            var toTarget = target.position - transform.position;
            toTarget.y = 0f;
            return toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : (dashDirection.sqrMagnitude > 0.001f ? dashDirection : transform.forward);
        }

        private void SpawnSlashTelegraph(float duration)
        {
            var preset = AnchorChainWeapon.VisualPreset.CreateDefault();
            preset.tint = new Color(0.88f, 0.98f, 0.86f, 0.46f);
            preset.heightOffset = 0.06f;
            preset.outerRadiusMultiplier = 1f;
            preset.innerRadiusFactor = 0.06f;
            preset.duration = duration;
            preset.endScaleMultiplier = 1.03f;
            preset.sortingOrder = 16;

            AnchorChainArcVisual.Spawn(
                transform.position,
                dashDirection,
                slashRange,
                slashAngle,
                0f,
                worldCamera,
                new AnchorChainWeapon.VisualResolved(preset));
        }
    }
}
