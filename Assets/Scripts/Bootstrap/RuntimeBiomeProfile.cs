using UnityEngine;

namespace Dagon.Bootstrap
{
    public sealed class RuntimeBiomeProfile
    {
        public RuntimeBiomeProfile(
            string biomeId,
            string displayName,
            string bossDisplayName,
            string[] baseTileResourcePaths,
            string[] mediumAccentTileResourcePaths,
            string[] rareAccentTileResourcePaths,
            string[] overlayTileResourcePaths,
            string mireWretchSpritePath,
            string drownedAcolyteSpritePath,
            string mermaidSpritePath,
            string watcherEyeSpritePath,
            string slimeSpritePath,
            string parasiteSpritePath,
            string deepSpawnSpritePath,
            string bossSpritePath,
            Color groundTint,
            Color overlayTint,
            Color propTint,
            Color enemyTint,
            Color bossTint,
            int drownedAcolyteSpawnEvery,
            int mermaidSpawnEvery,
            int watcherEyeSpawnEvery,
            int deepSpawnSpawnEvery,
            float bossTransitionDelaySeconds,
            float spawnIntervalReductionBonus,
            int additionalAliveCap)
        {
            BiomeId = biomeId;
            DisplayName = displayName;
            BossDisplayName = bossDisplayName;
            BaseTileResourcePaths = baseTileResourcePaths ?? System.Array.Empty<string>();
            MediumAccentTileResourcePaths = mediumAccentTileResourcePaths ?? System.Array.Empty<string>();
            RareAccentTileResourcePaths = rareAccentTileResourcePaths ?? System.Array.Empty<string>();
            OverlayTileResourcePaths = overlayTileResourcePaths ?? System.Array.Empty<string>();
            MireWretchSpritePath = mireWretchSpritePath;
            DrownedAcolyteSpritePath = drownedAcolyteSpritePath;
            MermaidSpritePath = mermaidSpritePath;
            WatcherEyeSpritePath = watcherEyeSpritePath;
            SlimeSpritePath = slimeSpritePath;
            ParasiteSpritePath = parasiteSpritePath;
            DeepSpawnSpritePath = deepSpawnSpritePath;
            BossSpritePath = bossSpritePath;
            GroundTint = groundTint;
            OverlayTint = overlayTint;
            PropTint = propTint;
            EnemyTint = enemyTint;
            BossTint = bossTint;
            DrownedAcolyteSpawnEvery = Mathf.Max(2, drownedAcolyteSpawnEvery);
            MermaidSpawnEvery = Mathf.Max(4, mermaidSpawnEvery);
            WatcherEyeSpawnEvery = Mathf.Max(4, watcherEyeSpawnEvery);
            DeepSpawnSpawnEvery = Mathf.Max(3, deepSpawnSpawnEvery);
            BossTransitionDelaySeconds = Mathf.Max(5f, bossTransitionDelaySeconds);
            SpawnIntervalReductionBonus = Mathf.Max(0f, spawnIntervalReductionBonus);
            AdditionalAliveCap = Mathf.Max(0, additionalAliveCap);
        }

        public string BiomeId { get; }
        public string DisplayName { get; }
        public string BossDisplayName { get; }
        public string[] BaseTileResourcePaths { get; }
        public string[] MediumAccentTileResourcePaths { get; }
        public string[] RareAccentTileResourcePaths { get; }
        public string[] OverlayTileResourcePaths { get; }
        public string MireWretchSpritePath { get; }
        public string DrownedAcolyteSpritePath { get; }
        public string MermaidSpritePath { get; }
        public string WatcherEyeSpritePath { get; }
        public string SlimeSpritePath { get; }
        public string ParasiteSpritePath { get; }
        public string DeepSpawnSpritePath { get; }
        public string BossSpritePath { get; }
        public Color GroundTint { get; }
        public Color OverlayTint { get; }
        public Color PropTint { get; }
        public Color EnemyTint { get; }
        public Color BossTint { get; }
        public int DrownedAcolyteSpawnEvery { get; }
        public int MermaidSpawnEvery { get; }
        public int WatcherEyeSpawnEvery { get; }
        public int DeepSpawnSpawnEvery { get; }
        public float BossTransitionDelaySeconds { get; }
        public float SpawnIntervalReductionBonus { get; }
        public int AdditionalAliveCap { get; }

        public static RuntimeBiomeProfile[] CreateDefaultSequence()
        {
            var baseTiles = new[]
            {
                "Sprites/Tiles/mire_base_a",
                "Sprites/Tiles/mire_base_b",
                "Sprites/Tiles/mire_base_c"
            };
            var mediumAccents = new[]
            {
                "Sprites/Tiles/mire_roots_a",
                "Sprites/Tiles/mire_puddle_a"
            };
            var rareAccents = new[]
            {
                "Sprites/Tiles/mire_bones_a"
            };
            var overlays = new[]
            {
                "Sprites/Tiles/mire_overlay_a",
                "Sprites/Tiles/mire_overlay_b"
            };

            return new[]
            {
                new RuntimeBiomeProfile(
                    "biome.spawn_refuge",
                    "Spawn Refuge",
                    "Mire Colossus",
                    baseTiles,
                    mediumAccents,
                    rareAccents,
                    overlays,
                    "Sprites/Enemies/mire_wretch",
                    "Sprites/Enemies/drowned_acolyte",
                    "Sprites/Enemies/mermaid",
                    "Sprites/Enemies/watcher_eye",
                    "Sprites/Enemies/slime",
                    "Sprites/Enemies/parasite",
                    "Sprites/Enemies/deep_spawn",
                    "Sprites/Bosses/mire_colossus",
                    new Color(0.82f, 0.88f, 0.80f, 1f),
                    new Color(0.78f, 0.84f, 0.78f, 0.90f),
                    new Color(0.84f, 0.90f, 0.82f, 0.92f),
                    new Color(0.88f, 0.92f, 0.86f, 1f),
                    Color.white,
                    7,
                    10,
                    8,
                    14,
                    45f,
                    0f,
                    0),
                new RuntimeBiomeProfile(
                    "biome.black_mire",
                    "Black Mire",
                    "Mire Colossus",
                    baseTiles,
                    mediumAccents,
                    rareAccents,
                    overlays,
                    "Sprites/Enemies/mire_wretch",
                    "Sprites/Enemies/drowned_acolyte",
                    "Sprites/Enemies/mermaid",
                    "Sprites/Enemies/watcher_eye",
                    "Sprites/Enemies/slime",
                    "Sprites/Enemies/parasite",
                    "Sprites/Enemies/deep_spawn",
                    "Sprites/Bosses/mire_colossus",
                    new Color(1f, 1f, 1f, 1f),
                    new Color(1f, 1f, 1f, 0.92f),
                    new Color(1f, 1f, 1f, 0.92f),
                    Color.white,
                    Color.white,
                    5,
                    8,
                    6,
                    12,
                    45f,
                    0.05f,
                    0),
                new RuntimeBiomeProfile(
                    "biome.corrupted_expansion",
                    "Corrupted Expanse",
                    "Tidebound Colossus",
                    baseTiles,
                    mediumAccents,
                    rareAccents,
                    overlays,
                    "Sprites/Enemies/mire_wretch",
                    "Sprites/Enemies/drowned_acolyte",
                    "Sprites/Enemies/mermaid",
                    "Sprites/Enemies/watcher_eye",
                    "Sprites/Enemies/slime",
                    "Sprites/Enemies/parasite",
                    "Sprites/Enemies/deep_spawn",
                    "Sprites/Bosses/mire_colossus",
                    new Color(0.58f, 0.70f, 0.64f, 1f),
                    new Color(0.54f, 0.66f, 0.62f, 0.94f),
                    new Color(0.62f, 0.74f, 0.68f, 0.92f),
                    new Color(0.76f, 0.86f, 0.80f, 1f),
                    new Color(0.76f, 0.88f, 0.82f, 1f),
                    4,
                    7,
                    5,
                    10,
                    42f,
                    0.14f,
                    1),
                new RuntimeBiomeProfile(
                    "biome.abyssal_reach",
                    "Abyssal Reach",
                    "Ossuary Colossus",
                    baseTiles,
                    mediumAccents,
                    rareAccents,
                    overlays,
                    "Sprites/Enemies/mire_wretch",
                    "Sprites/Enemies/drowned_acolyte",
                    "Sprites/Enemies/mermaid",
                    "Sprites/Enemies/watcher_eye",
                    "Sprites/Enemies/slime",
                    "Sprites/Enemies/parasite",
                    "Sprites/Enemies/deep_spawn",
                    "Sprites/Bosses/mire_colossus",
                    new Color(0.34f, 0.40f, 0.44f, 1f),
                    new Color(0.30f, 0.36f, 0.40f, 0.96f),
                    new Color(0.42f, 0.48f, 0.52f, 0.92f),
                    new Color(0.78f, 0.84f, 0.88f, 1f),
                    new Color(0.82f, 0.88f, 0.92f, 1f),
                    3,
                    6,
                    4,
                    8,
                    36f,
                    0.24f,
                    2)
            };
        }
    }
}
