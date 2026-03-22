using System.Collections.Generic;
using System.Linq;
using Dagon.Data;
using Dagon.Gameplay;
using UnityEngine;

namespace Dagon.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class DeveloperSandboxController : MonoBehaviour
    {
        [SerializeField] private PlayerCombatLoadout combatLoadout;
        [SerializeField] private SpawnDirector spawnDirector;
        [SerializeField] private KeyCode togglePanelKey = KeyCode.F1;

        private readonly List<WeaponDefinition> availableWeapons = new();
        private readonly HashSet<string> selectedWeaponIds = new();
        private Vector2 scrollPosition;
        private bool panelVisible = true;
        private int spawnBoostCount;
        private int manualSpawnCount;

        private void Start()
        {
            if (combatLoadout == null)
            {
                combatLoadout = FindObjectOfType<PlayerCombatLoadout>();
            }

            if (spawnDirector == null)
            {
                spawnDirector = FindObjectOfType<SpawnDirector>();
            }

            RebuildWeaponCatalog();
            SyncSelectionFromLoadout();
        }

        private void Update()
        {
            if (Input.GetKeyDown(togglePanelKey))
            {
                panelVisible = !panelVisible;
            }
        }

        private void OnGUI()
        {
            if (!panelVisible || combatLoadout == null || availableWeapons.Count == 0)
            {
                return;
            }

            const float width = 336f;
            const float rowHeight = 28f;
            var panelHeight = Mathf.Min(Screen.height - 32f, 324f + (availableWeapons.Count * rowHeight));
            var rect = new Rect(Screen.width - width - 16f, 16f, width, panelHeight);

            GUI.Box(rect, "Developer Sandbox");
            GUI.Label(new Rect(rect.x + 12f, rect.y + 26f, rect.width - 24f, 20f), $"Toggle panel: {togglePanelKey}");
            GUI.Label(new Rect(rect.x + 12f, rect.y + 48f, rect.width - 24f, 20f), "Choose your live weapon loadout.");

            if (GUI.Button(new Rect(rect.x + 12f, rect.y + 76f, 96f, 24f), "Base Only"))
            {
                EquipBaseOnly();
            }

            if (GUI.Button(new Rect(rect.x + 118f, rect.y + 76f, 96f, 24f), "All Weapons"))
            {
                EquipAllWeapons();
            }

            if (GUI.Button(new Rect(rect.x + 224f, rect.y + 76f, 100f, 24f), "Refresh"))
            {
                RebuildWeaponCatalog();
                SyncSelectionFromLoadout();
            }

            if (GUI.Button(new Rect(rect.x + 12f, rect.y + 104f, 152f, 24f), "Upgrade A All"))
            {
                UpgradeAllActiveWeapons(WeaponUpgradePath.PathA);
            }

            if (GUI.Button(new Rect(rect.x + 172f, rect.y + 104f, 152f, 24f), "Upgrade B All"))
            {
                UpgradeAllActiveWeapons(WeaponUpgradePath.PathB);
            }

            GUI.Label(new Rect(rect.x + 12f, rect.y + 136f, rect.width - 24f, 36f), $"Active: {string.Join(", ", combatLoadout.Weapons.Select(weapon => weapon.DisplayName))}");
            if (GUI.Button(new Rect(rect.x + 12f, rect.y + 172f, rect.width - 24f, 24f), $"Increase Spawn Pressure ({spawnBoostCount})"))
            {
                IncreaseSpawnPressure();
            }

            if (GUI.Button(new Rect(rect.x + 12f, rect.y + 202f, rect.width - 24f, 24f), GetAutoSpawnToggleLabel()))
            {
                ToggleAutoSpawning();
            }

            GUI.Label(new Rect(rect.x + 12f, rect.y + 232f, rect.width - 24f, 18f), $"Manual enemy spawns: {manualSpawnCount}");

            if (GUI.Button(new Rect(rect.x + 12f, rect.y + 254f, 152f, 24f), "Spawn Wretch"))
            {
                SpawnEnemy(SpawnEnemyKind.MireWretch);
            }

            if (GUI.Button(new Rect(rect.x + 172f, rect.y + 254f, 152f, 24f), "Spawn Acolyte"))
            {
                SpawnEnemy(SpawnEnemyKind.DrownedAcolyte);
            }

            if (GUI.Button(new Rect(rect.x + 12f, rect.y + 282f, 152f, 24f), "Spawn Mermaid"))
            {
                SpawnEnemy(SpawnEnemyKind.Mermaid);
            }

            if (GUI.Button(new Rect(rect.x + 172f, rect.y + 282f, 152f, 24f), "Spawn Deep Spawn"))
            {
                SpawnEnemy(SpawnEnemyKind.DeepSpawn);
            }

            if (GUI.Button(new Rect(rect.x + 12f, rect.y + 310f, rect.width - 24f, 24f), "Spawn Eye"))
            {
                SpawnEnemy(SpawnEnemyKind.WatcherEye);
            }

            var viewRect = new Rect(rect.x + 12f, rect.y + 342f, rect.width - 24f, rect.height - 354f);
            var contentRect = new Rect(0f, 0f, viewRect.width - 18f, availableWeapons.Count * rowHeight);
            scrollPosition = GUI.BeginScrollView(viewRect, scrollPosition, contentRect);

            for (var index = 0; index < availableWeapons.Count; index++)
            {
                DrawWeaponRow(index, availableWeapons[index], contentRect.width);
            }

            GUI.EndScrollView();
        }

        public void Configure(PlayerCombatLoadout loadout)
        {
            combatLoadout = loadout;
            if (spawnDirector == null)
            {
                spawnDirector = FindObjectOfType<SpawnDirector>();
            }
            RebuildWeaponCatalog();
            SyncSelectionFromLoadout();
        }

        private void DrawWeaponRow(int index, WeaponDefinition definition, float width)
        {
            var top = index * 28f;
            var isSelected = selectedWeaponIds.Contains(definition.WeaponId);
            var isBaseWeapon = combatLoadout.BaseWeapon != null && combatLoadout.BaseWeapon.WeaponId == definition.WeaponId;
            var buttonLabel = isSelected ? "Remove" : "Add";
            var nameLabel = isBaseWeapon ? $"{definition.DisplayName} [Base]" : definition.DisplayName;
            var runtimeWeapon = combatLoadout.GetWeapon(definition.WeaponId);

            GUI.Label(new Rect(0f, top + 4f, width - 190f, 20f), nameLabel);

            if (GUI.Button(new Rect(width - 186f, top + 2f, 42f, 24f), "Solo"))
            {
                EquipOnly(definition);
            }

            if (GUI.Button(new Rect(width - 140f, top + 2f, 42f, 24f), buttonLabel))
            {
                ToggleWeapon(definition);
            }

            GUI.enabled = runtimeWeapon != null;
            if (GUI.Button(new Rect(width - 94f, top + 2f, 42f, 24f), "A+"))
            {
                runtimeWeapon?.ApplySandboxPathUpgrade(WeaponUpgradePath.PathA);
            }

            if (GUI.Button(new Rect(width - 48f, top + 2f, 42f, 24f), "B+"))
            {
                runtimeWeapon?.ApplySandboxPathUpgrade(WeaponUpgradePath.PathB);
            }
            GUI.enabled = true;
        }

        private void RebuildWeaponCatalog()
        {
            availableWeapons.Clear();
            if (combatLoadout == null)
            {
                return;
            }

            var definitions = new List<WeaponDefinition>();
            if (combatLoadout.BaseWeapon?.Definition != null)
            {
                definitions.Add(combatLoadout.BaseWeapon.Definition);
            }

            definitions.AddRange(combatLoadout.WeaponPool.Where(definition => definition != null));

            foreach (var definition in definitions.GroupBy(definition => definition.WeaponId).Select(group => group.First()))
            {
                availableWeapons.Add(definition);
            }
        }

        private void SyncSelectionFromLoadout()
        {
            selectedWeaponIds.Clear();
            if (combatLoadout == null)
            {
                return;
            }

            foreach (var weapon in combatLoadout.Weapons)
            {
                if (weapon != null)
                {
                    selectedWeaponIds.Add(weapon.WeaponId);
                }
            }
        }

        private void EquipBaseOnly()
        {
            if (availableWeapons.Count == 0)
            {
                return;
            }

            selectedWeaponIds.Clear();
            selectedWeaponIds.Add(availableWeapons[0].WeaponId);
            combatLoadout.SetWeapons(new[] { availableWeapons[0] }, availableWeapons[0].WeaponId);
        }

        private void EquipAllWeapons()
        {
            if (availableWeapons.Count == 0)
            {
                return;
            }

            selectedWeaponIds.Clear();
            for (var index = 0; index < availableWeapons.Count; index++)
            {
                selectedWeaponIds.Add(availableWeapons[index].WeaponId);
            }

            combatLoadout.SetWeapons(availableWeapons, availableWeapons[0].WeaponId);
        }

        private void EquipOnly(WeaponDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            selectedWeaponIds.Clear();
            selectedWeaponIds.Add(definition.WeaponId);
            combatLoadout.SetWeapons(new[] { definition }, definition.WeaponId);
        }

        private void ToggleWeapon(WeaponDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            if (selectedWeaponIds.Contains(definition.WeaponId))
            {
                if (selectedWeaponIds.Count <= 1)
                {
                    return;
                }

                selectedWeaponIds.Remove(definition.WeaponId);
            }
            else
            {
                selectedWeaponIds.Add(definition.WeaponId);
            }

            var chosenDefinitions = availableWeapons.Where(weapon => selectedWeaponIds.Contains(weapon.WeaponId)).ToList();
            if (chosenDefinitions.Count == 0)
            {
                EquipBaseOnly();
                return;
            }

            var baseWeaponId = combatLoadout.BaseWeapon != null && selectedWeaponIds.Contains(combatLoadout.BaseWeapon.WeaponId)
                ? combatLoadout.BaseWeapon.WeaponId
                : chosenDefinitions[0].WeaponId;
            combatLoadout.SetWeapons(chosenDefinitions, baseWeaponId);
            SyncSelectionFromLoadout();
        }

        private void UpgradeAllActiveWeapons(WeaponUpgradePath path)
        {
            if (combatLoadout == null)
            {
                return;
            }

            foreach (var weapon in combatLoadout.Weapons)
            {
                weapon?.ApplySandboxPathUpgrade(path);
            }
        }

        private void IncreaseSpawnPressure()
        {
            if (spawnDirector == null)
            {
                spawnDirector = FindObjectOfType<SpawnDirector>();
            }

            if (spawnDirector == null)
            {
                return;
            }

            spawnDirector.IncreaseSandboxPressure();
            spawnBoostCount += 1;
        }

        private string GetAutoSpawnToggleLabel()
        {
            EnsureSpawnDirector();
            return spawnDirector != null && spawnDirector.IsAutoSpawningStopped ? "Start Auto Spawning" : "Stop Auto Spawning";
        }

        private void ToggleAutoSpawning()
        {
            EnsureSpawnDirector();
            if (spawnDirector == null)
            {
                return;
            }

            if (spawnDirector.IsAutoSpawningStopped)
            {
                spawnDirector.ResumeSpawning();
                return;
            }

            spawnDirector.StopSpawning();
        }

        private void SpawnEnemy(SpawnEnemyKind enemyKind)
        {
            EnsureSpawnDirector();
            if (spawnDirector == null)
            {
                return;
            }

            var spawned = enemyKind switch
            {
                SpawnEnemyKind.MireWretch => spawnDirector.SpawnSandboxMireWretch(),
                SpawnEnemyKind.DrownedAcolyte => spawnDirector.SpawnSandboxDrownedAcolyte(),
                SpawnEnemyKind.Mermaid => spawnDirector.SpawnSandboxMermaid(),
                SpawnEnemyKind.WatcherEye => spawnDirector.SpawnSandboxWatcherEye(),
                SpawnEnemyKind.DeepSpawn => spawnDirector.SpawnSandboxDeepSpawn(),
                _ => false
            };

            if (spawned)
            {
                manualSpawnCount += 1;
            }
        }

        private void EnsureSpawnDirector()
        {
            if (spawnDirector == null)
            {
                spawnDirector = FindObjectOfType<SpawnDirector>();
            }
        }

        private enum SpawnEnemyKind
        {
            MireWretch,
            DrownedAcolyte,
            Mermaid,
            WatcherEye,
            DeepSpawn
        }
    }
}
