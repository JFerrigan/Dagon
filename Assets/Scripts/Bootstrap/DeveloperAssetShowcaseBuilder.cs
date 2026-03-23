using System.Collections.Generic;
using Dagon.Core;
using Dagon.Gameplay;
using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class DeveloperAssetShowcaseBuilder : MonoBehaviour
    {
        private enum ShowcaseCategory
        {
            Characters,
            Enemies,
            Bosses,
            Props,
            Tiles,
            Pickups
        }

        private readonly struct ShowcaseEntry
        {
            public ShowcaseEntry(
                ShowcaseCategory category,
                string label,
                string resourcePath,
                Vector3 scale,
                float pixelsPerUnit = 64f,
                bool groundPlane = false,
                Vector3? visualOffset = null)
            {
                Category = category;
                Label = label;
                ResourcePath = resourcePath;
                Scale = scale;
                PixelsPerUnit = pixelsPerUnit;
                GroundPlane = groundPlane;
                VisualOffset = visualOffset ?? Vector3.zero;
            }

            public ShowcaseCategory Category { get; }
            public string Label { get; }
            public string ResourcePath { get; }
            public Vector3 Scale { get; }
            public float PixelsPerUnit { get; }
            public bool GroundPlane { get; }
            public Vector3 VisualOffset { get; }
        }

        [SerializeField] private Camera worldCamera;
        [SerializeField] private Vector3 origin = new(24f, 0.25f, -10f);
        [SerializeField] private int columns = 4;
        [SerializeField] private float horizontalSpacing = 4.2f;
        [SerializeField] private float verticalSpacing = 5.4f;
        [SerializeField] private float categorySpacing = 4.5f;

        public void Build(Camera cameraReference, Transform parentRoot)
        {
            worldCamera = cameraReference;

            var existing = transform.Find("DeveloperAssetShowcase");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            var root = new GameObject("DeveloperAssetShowcase");
            root.transform.SetParent(parentRoot != null ? parentRoot : transform, false);
            root.transform.localPosition = origin;

            var entries = BuildEntries();

            var groupedEntries = new Dictionary<ShowcaseCategory, List<ShowcaseEntry>>();
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (!groupedEntries.TryGetValue(entry.Category, out var group))
                {
                    group = new List<ShowcaseEntry>();
                    groupedEntries.Add(entry.Category, group);
                }

                group.Add(entry);
            }

            var categoryOrder = new[]
            {
                ShowcaseCategory.Characters,
                ShowcaseCategory.Enemies,
                ShowcaseCategory.Bosses,
                ShowcaseCategory.Props,
                ShowcaseCategory.Tiles,
                ShowcaseCategory.Pickups
            };

            var zOffset = 0f;
            for (var categoryIndex = 0; categoryIndex < categoryOrder.Length; categoryIndex++)
            {
                var category = categoryOrder[categoryIndex];
                if (!groupedEntries.TryGetValue(category, out var categoryEntries))
                {
                    continue;
                }

                CreateCategoryHeader(root.transform, category, zOffset);
                for (var entryIndex = 0; entryIndex < categoryEntries.Count; entryIndex++)
                {
                    CreateEntry(root.transform, categoryEntries[entryIndex], entryIndex, zOffset + 2.2f);
                }

                var rows = Mathf.CeilToInt(categoryEntries.Count / (float)Mathf.Max(1, columns));
                zOffset += (rows * verticalSpacing) + categorySpacing;
            }
        }

        private void CreateEntry(Transform parent, ShowcaseEntry entry, int index, float zOffset)
        {
            var sprite = RuntimeSpriteLibrary.LoadSprite(entry.ResourcePath, entry.PixelsPerUnit);
            if (sprite == null)
            {
                return;
            }

            var column = index % Mathf.Max(1, columns);
            var row = index / Mathf.Max(1, columns);
            var position = new Vector3(column * horizontalSpacing, 0f, zOffset + (row * verticalSpacing));

            var anchor = new GameObject(entry.Label);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = position;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(anchor.transform, false);
            visual.transform.localScale = entry.GroundPlane
                ? entry.Scale
                : ResolveAspectPreservingScale(sprite, entry.Scale);

            var spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = 6;

            if (entry.GroundPlane)
            {
                visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                visual.transform.localPosition = entry.VisualOffset;
                spriteRenderer.sortingOrder = -10;
            }
            else
            {
                var billboard = visual.AddComponent<BillboardSprite>();
                billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);
                visual.transform.localPosition = entry.VisualOffset;
            }

            CreateLabel(anchor.transform, entry.Label, entry.GroundPlane ? -0.28f : -0.5f);
            CreateBasePlate(anchor.transform, Mathf.Max(1.4f, sprite.bounds.size.x * Mathf.Max(visual.transform.localScale.x, visual.transform.localScale.y) * 1.2f));
            anchor.transform.localPosition += Vector3.up * 0.02f;
        }

        private static List<ShowcaseEntry> BuildEntries()
        {
            var entries = new List<ShowcaseEntry>();
            AddCharacterEntries(entries);
            AddEnemyEntries(entries);
            AddBossEntries(entries);
            AddPropEntries(entries);
            AddTileEntries(entries);
            AddPickupEntries(entries);
            return entries;
        }

        private static void AddCharacterEntries(List<ShowcaseEntry> entries)
        {
            var profiles = RuntimeCharacterCatalog.GetCharacterProfiles();
            for (var index = 0; index < profiles.Length; index++)
            {
                var profile = profiles[index];
                if (profile == null || string.IsNullOrWhiteSpace(profile.RuntimeSpritePath))
                {
                    continue;
                }

                entries.Add(new ShowcaseEntry(
                    ShowcaseCategory.Characters,
                    profile.DisplayName,
                    profile.RuntimeSpritePath,
                    Vector3.one * profile.RuntimeScale));
            }
        }

        private static void AddEnemyEntries(List<ShowcaseEntry> entries)
        {
            AddEnemyEntry(entries, "Mire Wretch", "mire_wretch");
            AddEnemyEntry(entries, "Drowned Acolyte", "drowned_acolyte");
            AddEnemyEntry(entries, "Mermaid", "mermaid");
            AddEnemyEntry(entries, "Deep Spawn", "deep_spawn");
            AddEnemyEntry(entries, "Parasite", "parasite");
            AddEnemyEntry(entries, "Watcher Eye", "watcher_eye");
            AddEnemyEntry(entries, "Tall Leech", "tall_leech");
            AddEnemyEntry(entries, "Wide Leech", "wide_leech");
        }

        private static void AddEnemyEntry(List<ShowcaseEntry> entries, string label, string enemyId)
        {
            if (!SpawnDirector.TryGetEnemyVisualSpec(enemyId, out var spec))
            {
                return;
            }

            entries.Add(new ShowcaseEntry(
                ShowcaseCategory.Enemies,
                label,
                spec.ResourcePath,
                spec.Scale,
                spec.PixelsPerUnit,
                false,
                spec.LocalPosition));
        }

        private static void AddBossEntries(List<ShowcaseEntry> entries)
        {
            AddBossEntry(entries, "Mire Colossus", "mire_colossus");
            AddBossEntry(entries, "Monolith", "monolith");
            AddBossEntry(entries, "Drowned Admiral", "drowned_admiral");
        }

        private static void AddBossEntry(List<ShowcaseEntry> entries, string label, string bossId)
        {
            if (!RunStateManager.TryGetBossVisualSpec(bossId, out var spec))
            {
                return;
            }

            entries.Add(new ShowcaseEntry(
                ShowcaseCategory.Bosses,
                label,
                spec.ResourcePath,
                spec.Scale,
                spec.PixelsPerUnit,
                false,
                spec.LocalPosition));
        }

        private static void AddPropEntries(List<ShowcaseEntry> entries)
        {
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Props, "Barrel Prop", "Sprites/Props/barrel_ground_prop", Vector3.one * 0.11f));
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Props, "Carcass Prop", "Sprites/Props/carcass2_prop", Vector3.one * 0.15f));
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Props, "Fish Pile", "Sprites/Props/fish_pile_prop", Vector3.one * 0.11f));
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Props, "Harpoon Prop", "Sprites/Props/harpoon_ground_prop", Vector3.one * 0.05f));
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Props, "Leviathan Prop", "Sprites/Props/leviathan_carcass_prop", Vector3.one * 0.16f));
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Props, "Skull Mound", "Sprites/Props/skull_mound_prop", Vector3.one * 0.15f));
        }

        private static void AddTileEntries(List<ShowcaseEntry> entries)
        {
            var tileScale = new Vector3(1f, 1f, 1f);
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Tiles, "Mire Base A", "Sprites/Tiles/mire_base_a", tileScale, 64f, true));
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Tiles, "Mire Base B", "Sprites/Tiles/mire_base_b", tileScale, 64f, true));
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Tiles, "Mire Base C", "Sprites/Tiles/mire_base_c", tileScale, 64f, true));
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Tiles, "Mire Bones", "Sprites/Tiles/mire_bones_a", tileScale, 64f, true));
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Tiles, "Mire Overlay A", "Sprites/Tiles/mire_overlay_a", tileScale, 64f, true));
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Tiles, "Mire Overlay B", "Sprites/Tiles/mire_overlay_b", tileScale, 64f, true));
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Tiles, "Mire Puddle", "Sprites/Tiles/mire_puddle_a", tileScale, 64f, true));
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Tiles, "Mire Roots", "Sprites/Tiles/mire_roots_a", tileScale, 64f, true));
        }

        private static void AddPickupEntries(List<ShowcaseEntry> entries)
        {
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Pickups, "Barnacle Shard", "Sprites/Pickups/barnacle_shard", Vector3.one * 0.24f, 256f));
            entries.Add(new ShowcaseEntry(ShowcaseCategory.Pickups, "Health Pickup", "Sprites/UI/heart", Vector3.one * 2.7f, 256f));
        }

        private void CreateCategoryHeader(Transform parent, ShowcaseCategory category, float zOffset)
        {
            var header = new GameObject($"{category}Header");
            header.transform.SetParent(parent, false);
            header.transform.localPosition = new Vector3(((Mathf.Max(1, columns) - 1) * horizontalSpacing) * 0.5f, 2.1f, zOffset);

            var mesh = header.AddComponent<TextMesh>();
            mesh.text = category switch
            {
                ShowcaseCategory.Characters => "CHARACTERS",
                ShowcaseCategory.Enemies => "ENEMIES",
                ShowcaseCategory.Bosses => "BOSSES",
                ShowcaseCategory.Props => "PROPS",
                ShowcaseCategory.Tiles => "TILES & OVERLAYS",
                _ => "PICKUPS"
            };
            mesh.fontSize = 54;
            mesh.characterSize = 0.16f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.96f, 0.88f, 0.68f, 1f);

            var renderer = header.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 10;
            }

            var billboard = header.AddComponent<BillboardSprite>();
            billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);
        }

        private void CreateLabel(Transform parent, string label, float yOffset)
        {
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, yOffset, 0f);

            var mesh = labelObject.AddComponent<TextMesh>();
            mesh.text = label;
            mesh.fontSize = 36;
            mesh.characterSize = 0.12f;
            mesh.anchor = TextAnchor.UpperCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.88f, 0.93f, 0.86f, 1f);

            var renderer = labelObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 7;
            }

            var billboard = labelObject.AddComponent<BillboardSprite>();
            billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);
        }

        private static void CreateBasePlate(Transform parent, float width)
        {
            var plate = new GameObject("Plate");
            plate.transform.SetParent(parent, false);
            plate.transform.localPosition = new Vector3(0f, 0f, 0f);
            plate.transform.localScale = new Vector3(width, 1f, 1f);

            var renderer = plate.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Effects/brine_surge", 256f);
            renderer.color = new Color(0.30f, 0.44f, 0.38f, 0.18f);
            renderer.sortingOrder = 1;
            plate.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private static Vector3 ResolveAspectPreservingScale(Sprite sprite, Vector3 requestedScale)
        {
            if (sprite == null)
            {
                return requestedScale;
            }

            var spriteWidth = Mathf.Max(1f, sprite.rect.width);
            var spriteHeight = Mathf.Max(1f, sprite.rect.height);
            var spriteAspect = spriteWidth / spriteHeight;

            var maxWidth = Mathf.Max(0.0001f, requestedScale.x);
            var maxHeight = Mathf.Max(0.0001f, requestedScale.y);

            var resolvedWidth = maxWidth;
            var resolvedHeight = resolvedWidth / spriteAspect;

            if (resolvedHeight > maxHeight)
            {
                resolvedHeight = maxHeight;
                resolvedWidth = resolvedHeight * spriteAspect;
            }

            return new Vector3(resolvedWidth, resolvedHeight, requestedScale.z);
        }
    }
}
