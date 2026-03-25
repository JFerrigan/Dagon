using System.Collections.Generic;
using Dagon.Bootstrap;
using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class WorldProgressionDirector : MonoBehaviour
    {
        public readonly struct BiomeSample
        {
            public BiomeSample(
                RuntimeBiomeProfile primaryProfile,
                int primaryIndex,
                RuntimeBiomeProfile secondaryProfile,
                int secondaryIndex,
                float secondaryBlend)
            {
                PrimaryProfile = primaryProfile;
                PrimaryIndex = primaryIndex;
                SecondaryProfile = secondaryProfile;
                SecondaryIndex = secondaryIndex;
                SecondaryBlend = Mathf.Clamp01(secondaryBlend);
            }

            public RuntimeBiomeProfile PrimaryProfile { get; }
            public int PrimaryIndex { get; }
            public RuntimeBiomeProfile SecondaryProfile { get; }
            public int SecondaryIndex { get; }
            public float SecondaryBlend { get; }
            public bool HasSecondaryProfile =>
                SecondaryProfile != null &&
                SecondaryIndex != PrimaryIndex &&
                SecondaryBlend > 0.001f;
        }

        private const float CorruptionOriginMinDistance = 180f;
        private const float CorruptionOriginMaxDistance = 300f;
        private const float StartingSafeRadius = 500f;

        [SerializeField] private Transform player;
        [SerializeField] private RunStateManager runStateManager;
        [SerializeField] private SpawnDirector spawnDirector;
        [SerializeField] private MireGroundTiler groundTiler;
        [SerializeField] private MirePropScatterer propScatterer;
        [SerializeField] private float biomeRegionCellSize = 112f;
        [SerializeField, Range(0f, 0.48f)] private float biomeRegionJitter = 0.38f;
        [SerializeField] private float biomeBlendDistance = 32f;
        [SerializeField] private float corruptionMoveSlowAmount = 0.4f;
        [SerializeField] private float corruptionAttackRatePenalty = 0.55f;
        [SerializeField] private float corruptionGainPerSecond = 1.2f;

        private readonly Dictionary<PlayerWeaponRuntime, float> appliedAttackRatePenalties = new();
        private RuntimeBiomeProfile[] biomeSequence = System.Array.Empty<RuntimeBiomeProfile>();
        private PlayerCombatLoadout playerCombatLoadout;
        private PlayerSlowReceiver playerSlowReceiver;
        private CorruptionMeter corruptionMeter;
        private Vector3 worldOrigin;
        private Vector3 corruptionOrigin;
        private Vector2 biomeFieldSeedOffset;
        private int biomeFieldSeedSalt;
        private float currentCorruptionRadius;
        private int currentBiomeIndex = -1;
        private bool corruptionPenaltyActive;

        public RuntimeBiomeProfile CurrentBiome =>
            biomeSequence != null && biomeSequence.Length > 0
                ? biomeSequence[Mathf.Clamp(currentBiomeIndex, 0, biomeSequence.Length - 1)]
                : null;

        public Vector3 WorldOrigin => worldOrigin;
        public Vector3 CorruptionOrigin => corruptionOrigin;
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
            corruptionOrigin = ResolveCorruptionOrigin(worldOrigin);
            InitializeBiomeFieldSeed();
            currentBiomeIndex = -1;
            currentCorruptionRadius = StartingSafeRadius;

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

            if (corruptionOrigin == Vector3.zero && player != null)
            {
                corruptionOrigin = ResolveCorruptionOrigin(worldOrigin);
            }

            if (biomeFieldSeedSalt == 0)
            {
                InitializeBiomeFieldSeed();
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
            return SampleBiomeAtPosition(worldPosition).PrimaryProfile;
        }

        public int ResolveBiomeIndexAtPosition(Vector3 worldPosition)
        {
            if (biomeSequence == null || biomeSequence.Length == 0)
            {
                return 0;
            }

            return Mathf.Clamp(SampleBiomeAtPosition(worldPosition).PrimaryIndex, 0, biomeSequence.Length - 1);
        }

        public BiomeSample SampleBiomeAtPosition(Vector3 worldPosition)
        {
            if (biomeSequence == null || biomeSequence.Length == 0)
            {
                return new BiomeSample(null, 0, null, 0, 0f);
            }

            var planar = Flatten(worldPosition);
            var seededPosition = new Vector2(planar.x + biomeFieldSeedOffset.x, planar.z + biomeFieldSeedOffset.y);
            var cellSize = Mathf.Max(8f, biomeRegionCellSize);
            var sourceCell = new Vector2Int(
                Mathf.FloorToInt(seededPosition.x / cellSize),
                Mathf.FloorToInt(seededPosition.y / cellSize));

            var primaryIndex = 0;
            var secondaryIndex = 0;
            var primaryDistanceSquared = float.MaxValue;
            var secondaryDistanceSquared = float.MaxValue;

            for (var z = -2; z <= 2; z++)
            {
                for (var x = -2; x <= 2; x++)
                {
                    var cell = new Vector2Int(sourceCell.x + x, sourceCell.y + z);
                    var sitePosition = ResolveBiomeSitePosition(cell, cellSize);
                    var distanceSquared = (sitePosition - seededPosition).sqrMagnitude;
                    var biomeIndex = ResolveBiomeIndexForCell(cell);

                    if (distanceSquared < primaryDistanceSquared)
                    {
                        secondaryDistanceSquared = primaryDistanceSquared;
                        secondaryIndex = primaryIndex;
                        primaryDistanceSquared = distanceSquared;
                        primaryIndex = biomeIndex;
                        continue;
                    }

                    if (distanceSquared < secondaryDistanceSquared)
                    {
                        secondaryDistanceSquared = distanceSquared;
                        secondaryIndex = biomeIndex;
                    }
                }
            }

            var primaryProfile = biomeSequence[Mathf.Clamp(primaryIndex, 0, biomeSequence.Length - 1)];
            var secondaryProfile =
                secondaryDistanceSquared < float.MaxValue && secondaryIndex != primaryIndex
                    ? biomeSequence[Mathf.Clamp(secondaryIndex, 0, biomeSequence.Length - 1)]
                    : null;
            var secondaryBlend = 0f;
            if (secondaryProfile != null)
            {
                var borderDelta = Mathf.Sqrt(secondaryDistanceSquared) - Mathf.Sqrt(primaryDistanceSquared);
                secondaryBlend = 1f - Mathf.Clamp01(borderDelta / Mathf.Max(0.01f, biomeBlendDistance));
            }

            return new BiomeSample(primaryProfile, primaryIndex, secondaryProfile, secondaryIndex, secondaryBlend);
        }

        public bool IsPositionCorrupted(Vector3 worldPosition)
        {
            return Vector3.Distance(Flatten(worldPosition), corruptionOrigin) >= currentCorruptionRadius;
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

            var biomeSample = SampleBiomeAtPosition(player.position);
            var nextBiomeIndex = biomeSample.PrimaryIndex;
            if (!force && nextBiomeIndex == currentBiomeIndex)
            {
                return;
            }

            currentBiomeIndex = nextBiomeIndex;
            var profile = biomeSequence[currentBiomeIndex];
            spawnDirector?.ConfigureBiome(profile);
            runStateManager?.ConfigureBiome(profile);
            groundTiler?.RefreshProgressionPresentation();
            propScatterer?.RefreshProgressionPresentation();

            Debug.Log(
                $"WorldProgressionDirector set active biome patch '{profile.DisplayName}' (index {currentBiomeIndex}) near {Flatten(player.position)}.",
                this);
        }

        private void RefreshCorruptionField(bool force)
        {
            if (player == null || corruptionMeter == null)
            {
                return;
            }

            var targetRadius = Mathf.Lerp(StartingSafeRadius, 0f, Mathf.Clamp01(corruptionMeter.CurrentCorruption / 250f));
            var nextRadius = Mathf.Max(0f, targetRadius);
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

        private void InitializeBiomeFieldSeed()
        {
            biomeFieldSeedOffset = new Vector2(Random.Range(-10000f, 10000f), Random.Range(-10000f, 10000f));
            biomeFieldSeedSalt = Random.Range(int.MinValue / 2, int.MaxValue / 2);
            if (biomeFieldSeedSalt == 0)
            {
                biomeFieldSeedSalt = 1;
            }
        }

        private int ResolveBiomeIndexForCell(Vector2Int cell)
        {
            return Mathf.Clamp(
                Mathf.FloorToInt(Hash01(cell.x, cell.y, biomeFieldSeedSalt) * biomeSequence.Length),
                0,
                biomeSequence.Length - 1);
        }

        private Vector2 ResolveBiomeSitePosition(Vector2Int cell, float cellSize)
        {
            var jitterRange = Mathf.Clamp01(biomeRegionJitter) * cellSize;
            var offsetX = Mathf.Lerp(-jitterRange, jitterRange, Hash01(cell.x, cell.y, biomeFieldSeedSalt + 17));
            var offsetY = Mathf.Lerp(-jitterRange, jitterRange, Hash01(cell.x, cell.y, biomeFieldSeedSalt + 53));
            return new Vector2((cell.x * cellSize) + offsetX, (cell.y * cellSize) + offsetY);
        }

        private static float Hash01(int x, int y, int salt)
        {
            unchecked
            {
                var hash = (uint)(x * 374761393) ^ (uint)(y * 668265263) ^ (uint)(salt * 1442695041);
                hash = (hash ^ (hash >> 13)) * 1274126177u;
                hash ^= hash >> 16;
                return (hash & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static Vector3 ResolveCorruptionOrigin(Vector3 playerOrigin)
        {
            var direction = Random.insideUnitCircle;
            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector2.right;
            }

            direction.Normalize();
            var distance = Random.Range(CorruptionOriginMinDistance, CorruptionOriginMaxDistance);
            return playerOrigin + new Vector3(direction.x, 0f, direction.y) * distance;
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
