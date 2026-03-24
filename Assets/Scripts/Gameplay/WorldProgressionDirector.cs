using System.Collections.Generic;
using Dagon.Bootstrap;
using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class WorldProgressionDirector : MonoBehaviour
    {
        private const float BossCorruptionSpreadMultiplier = 0.1f;

        [SerializeField] private Transform player;
        [SerializeField] private RunStateManager runStateManager;
        [SerializeField] private SpawnDirector spawnDirector;
        [SerializeField] private MireGroundTiler groundTiler;
        [SerializeField] private MirePropScatterer propScatterer;
        [SerializeField] private float sanctuaryRadius = 24f;
        [SerializeField] private float ringWidth = 48f;
        [SerializeField] private float corruptionStartDelaySeconds = 32f;
        [SerializeField] private float corruptionSpreadRate = 0.72f;
        [SerializeField] private float corruptionMoveSlowAmount = 0.4f;
        [SerializeField] private float corruptionAttackRatePenalty = 0.55f;
        [SerializeField] private float corruptionGainPerSecond = 1.2f;

        private readonly Dictionary<PlayerWeaponRuntime, float> appliedAttackRatePenalties = new();
        private RuntimeBiomeProfile[] biomeSequence = System.Array.Empty<RuntimeBiomeProfile>();
        private PlayerCombatLoadout playerCombatLoadout;
        private PlayerSlowReceiver playerSlowReceiver;
        private CorruptionMeter corruptionMeter;
        private Vector3 worldOrigin;
        private float runtimeStartedAt;
        private float lastCorruptionUpdateTime;
        private float currentCorruptionRadius;
        private int currentBiomeIndex = -1;
        private bool corruptionPenaltyActive;

        public RuntimeBiomeProfile CurrentBiome =>
            biomeSequence != null && biomeSequence.Length > 0
                ? biomeSequence[Mathf.Clamp(currentBiomeIndex, 0, biomeSequence.Length - 1)]
                : null;

        public Vector3 WorldOrigin => worldOrigin;
        public float CurrentCorruptionRadius => currentCorruptionRadius;

        public void Configure(
            Transform playerTransform,
            RunStateManager manager,
            SpawnDirector director,
            MireGroundTiler tiler,
            MirePropScatterer scatterer,
            RuntimeBiomeProfile[] sequence)
        {
            if (runStateManager != null)
            {
                runStateManager.BossWaveCompleted -= HandleBossWaveCompleted;
            }

            player = playerTransform;
            runStateManager = manager;
            spawnDirector = director;
            groundTiler = tiler;
            propScatterer = scatterer;
            biomeSequence = sequence ?? System.Array.Empty<RuntimeBiomeProfile>();
            playerCombatLoadout = player != null ? player.GetComponent<PlayerCombatLoadout>() : null;
            playerSlowReceiver = player != null ? player.GetComponent<PlayerSlowReceiver>() : null;
            corruptionMeter = player != null ? player.GetComponent<CorruptionMeter>() : null;
            worldOrigin = player != null ? Flatten(player.position) : Vector3.zero;
            runtimeStartedAt = Time.time;
            lastCorruptionUpdateTime = runtimeStartedAt;
            currentBiomeIndex = -1;
            currentCorruptionRadius = 0f;

            groundTiler?.ConfigureProgression(this);
            propScatterer?.ConfigureProgression(this);

            if (runStateManager != null && isActiveAndEnabled)
            {
                runStateManager.BossWaveCompleted += HandleBossWaveCompleted;
            }

            if (biomeSequence == null || biomeSequence.Length == 0)
            {
                biomeSequence = RuntimeBiomeProfile.CreateDefaultSequence();
            }

            RefreshPlayerBiome(force: true);
            RefreshCorruptionField(force: true);
        }

        private void Start()
        {
            if (biomeSequence == null || biomeSequence.Length == 0)
            {
                biomeSequence = RuntimeBiomeProfile.CreateDefaultSequence();
            }

            if (player != null && worldOrigin == Vector3.zero)
            {
                worldOrigin = Flatten(player.position);
            }

            if (runtimeStartedAt <= 0f)
            {
                runtimeStartedAt = Time.time;
            }
            if (lastCorruptionUpdateTime <= 0f)
            {
                lastCorruptionUpdateTime = runtimeStartedAt;
            }

            groundTiler?.ConfigureProgression(this);
            propScatterer?.ConfigureProgression(this);
            RefreshPlayerBiome(force: true);
            RefreshCorruptionField(force: true);
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            RefreshPlayerBiome(force: false);
            RefreshCorruptionField(force: false);
            UpdateCorruptionPenalty();
        }

        private void OnEnable()
        {
            if (runStateManager != null)
            {
                runStateManager.BossWaveCompleted += HandleBossWaveCompleted;
            }
        }

        private void OnDisable()
        {
            if (runStateManager != null)
            {
                runStateManager.BossWaveCompleted -= HandleBossWaveCompleted;
            }

            ClearCorruptionAttackPenalty();
        }

        public RuntimeBiomeProfile ResolveBiomeAtPosition(Vector3 worldPosition)
        {
            if (biomeSequence == null || biomeSequence.Length == 0)
            {
                return null;
            }

            var index = ResolveBiomeIndexAtPosition(worldPosition);
            return biomeSequence[Mathf.Clamp(index, 0, biomeSequence.Length - 1)];
        }

        public int ResolveBiomeIndexAtPosition(Vector3 worldPosition)
        {
            if (biomeSequence == null || biomeSequence.Length == 0)
            {
                return 0;
            }

            var planarDistance = Vector3.Distance(Flatten(worldPosition), worldOrigin);
            if (planarDistance <= sanctuaryRadius)
            {
                return 0;
            }

            if (ringWidth <= 0.01f)
            {
                return biomeSequence.Length - 1;
            }

            var bandIndex = 1 + Mathf.FloorToInt((planarDistance - sanctuaryRadius) / ringWidth);
            return Mathf.Clamp(bandIndex, 0, biomeSequence.Length - 1);
        }

        public bool IsPositionCorrupted(Vector3 worldPosition)
        {
            return currentCorruptionRadius > 0.01f &&
                Vector3.Distance(Flatten(worldPosition), worldOrigin) <= currentCorruptionRadius;
        }

        private void HandleBossWaveCompleted()
        {
            var currentProfile = CurrentBiome;
            var nextBossDelay = currentProfile != null ? currentProfile.BossTransitionDelaySeconds : 45f;
            runStateManager?.ResumeAmbientRun(nextBossDelay);
            spawnDirector?.ResumeSpawning();
        }

        private void RefreshPlayerBiome(bool force)
        {
            if (biomeSequence == null || biomeSequence.Length == 0 || player == null)
            {
                return;
            }

            var nextBiomeIndex = ResolveBiomeIndexAtPosition(player.position);
            if (!force && nextBiomeIndex == currentBiomeIndex)
            {
                return;
            }

            currentBiomeIndex = nextBiomeIndex;
            var profile = biomeSequence[currentBiomeIndex];
            spawnDirector?.ConfigureBiome(profile);
            spawnDirector?.ConfigureDistanceThreat(profile, currentBiomeIndex);
            runStateManager?.ConfigureBiome(profile);
            groundTiler?.RefreshProgressionPresentation();
            propScatterer?.RefreshProgressionPresentation();

            Debug.Log(
                $"WorldProgressionDirector set active ring '{profile.DisplayName}' (index {currentBiomeIndex}) at distance {Vector3.Distance(Flatten(player.position), worldOrigin):0.0}.",
                this);
        }

        private void RefreshCorruptionField(bool force)
        {
            if (player == null)
            {
                return;
            }

            var now = Time.time;
            if (lastCorruptionUpdateTime <= 0f)
            {
                lastCorruptionUpdateTime = now;
            }

            var startTime = runtimeStartedAt + corruptionStartDelaySeconds;
            var activeFrom = Mathf.Max(lastCorruptionUpdateTime, startTime);
            var activeDelta = Mathf.Max(0f, now - activeFrom);
            var bossMultiplier = runStateManager != null && runStateManager.BossWaveStarted
                ? BossCorruptionSpreadMultiplier
                : 1f;
            var nextRadius = currentCorruptionRadius + (activeDelta * corruptionSpreadRate * bossMultiplier);
            lastCorruptionUpdateTime = now;
            if (!force && Mathf.Abs(nextRadius - currentCorruptionRadius) < 0.35f)
            {
                return;
            }

            currentCorruptionRadius = nextRadius;
            groundTiler?.RefreshProgressionPresentation();
            propScatterer?.RefreshProgressionPresentation();
        }

        private void UpdateCorruptionPenalty()
        {
            var insideCorruption = IsPositionCorrupted(player.position);
            if (insideCorruption)
            {
                playerSlowReceiver?.ApplySlow(corruptionMoveSlowAmount, 0.2f);
                corruptionMeter?.AddCorruption(corruptionGainPerSecond * Time.deltaTime);
                RefreshCorruptionAttackPenalty();
                corruptionPenaltyActive = true;
                return;
            }

            if (!corruptionPenaltyActive)
            {
                return;
            }

            corruptionPenaltyActive = false;
            ClearCorruptionAttackPenalty();
        }

        private void RefreshCorruptionAttackPenalty()
        {
            if (playerCombatLoadout == null || corruptionAttackRatePenalty <= 0f)
            {
                return;
            }

            for (var index = 0; index < playerCombatLoadout.Weapons.Count; index++)
            {
                var weapon = playerCombatLoadout.Weapons[index];
                if (weapon == null || appliedAttackRatePenalties.ContainsKey(weapon))
                {
                    continue;
                }

                weapon.ModifyAttackRate(-corruptionAttackRatePenalty);
                appliedAttackRatePenalties[weapon] = corruptionAttackRatePenalty;
            }

            var staleWeapons = ListPool<PlayerWeaponRuntime>.Get();
            foreach (var pair in appliedAttackRatePenalties)
            {
                if (pair.Key != null)
                {
                    continue;
                }

                staleWeapons.Add(pair.Key);
            }

            for (var index = 0; index < staleWeapons.Count; index++)
            {
                appliedAttackRatePenalties.Remove(staleWeapons[index]);
            }

            ListPool<PlayerWeaponRuntime>.Release(staleWeapons);
        }

        private void ClearCorruptionAttackPenalty()
        {
            foreach (var pair in appliedAttackRatePenalties)
            {
                if (pair.Key != null)
                {
                    pair.Key.ModifyAttackRate(pair.Value);
                }
            }

            appliedAttackRatePenalties.Clear();
        }

        private static Vector3 Flatten(Vector3 position)
        {
            return new Vector3(position.x, 0f, position.z);
        }

        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new();

            public static List<T> Get()
            {
                return Pool.Count > 0 ? Pool.Pop() : new List<T>();
            }

            public static void Release(List<T> list)
            {
                if (list == null)
                {
                    return;
                }

                list.Clear();
                Pool.Push(list);
            }
        }
    }
}
