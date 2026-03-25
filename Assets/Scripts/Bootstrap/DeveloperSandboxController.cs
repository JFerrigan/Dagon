using System.Collections.Generic;
using System.Linq;
using Dagon.Core;
using Dagon.Data;
using Dagon.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace Dagon.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class DeveloperSandboxController : MonoBehaviour
    {
        [SerializeField] private PlayerCombatLoadout combatLoadout;
        [SerializeField] private Health playerHealth;
        [SerializeField] private SpawnDirector spawnDirector;
        [SerializeField] private CorruptionEventDirector corruptionEventDirector;
        [SerializeField] private RunStateManager runStateManager;
        [SerializeField] private CorruptionMeter corruptionMeter;
        [SerializeField] private KeyCode togglePanelKey = KeyCode.F1;

        private readonly List<WeaponDefinition> availableWeapons = new();
        private readonly HashSet<string> selectedWeaponIds = new();
        private CharacterProfileDefinition[] availableCharacterProfiles = System.Array.Empty<CharacterProfileDefinition>();
        private ActiveAbilityDefinition[] availableActiveDefinitions = System.Array.Empty<ActiveAbilityDefinition>();
        private Vector2 scrollPosition;
        private bool panelVisible = true;
        private bool playerInvincible;
        private bool corruptManualSpawns;
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

            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<Health>();
            }

            if (corruptionEventDirector == null)
            {
                corruptionEventDirector = FindObjectOfType<CorruptionEventDirector>();
            }

            if (runStateManager == null)
            {
                runStateManager = FindObjectOfType<RunStateManager>();
            }

            if (corruptionMeter == null)
            {
                corruptionMeter = FindObjectOfType<CorruptionMeter>();
            }

            RebuildWeaponCatalog();
            SyncSelectionFromLoadout();
            availableCharacterProfiles = RuntimeCharacterCatalog.GetCharacterProfiles();
            availableActiveDefinitions = RuntimeCharacterCatalog.GetSandboxActivePool();
        }

        private void Update()
        {
            if (WasToggleKeyPressed(togglePanelKey))
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
            const float headerHeight = 28f;
            const float panelPadding = 12f;
            const float contentTop = 16f;
            var panelHeight = Mathf.Min(Screen.height - 32f, 640f);
            var rect = new Rect(Screen.width - width - 16f, 16f, width, panelHeight);
            var currentProfile = ResolveCurrentCharacterProfile();
            var currentActive = combatLoadout.GetPrimaryActive();
            var currentCorruption = corruptionMeter != null ? corruptionMeter.CurrentCorruption : 0f;
            var characterRows = Mathf.Max(1, Mathf.CeilToInt(availableCharacterProfiles.Length / 2f));
            var abilityRows = Mathf.Max(1, Mathf.CeilToInt(availableActiveDefinitions.Length / 2f));
            var contentHeight = 892f
                + (characterRows * rowHeight)
                + (abilityRows * rowHeight)
                + (availableWeapons.Count * rowHeight);

            GUI.Box(rect, "Developer Sandbox");
            var viewRect = new Rect(
                rect.x + panelPadding,
                rect.y + headerHeight,
                rect.width - (panelPadding * 2f),
                rect.height - headerHeight - panelPadding);
            var contentRect = new Rect(0f, 0f, viewRect.width - 18f, contentHeight);
            scrollPosition = GUI.BeginScrollView(viewRect, scrollPosition, contentRect);

            GUI.Label(new Rect(0f, contentTop, contentRect.width, 20f), $"Toggle panel: {togglePanelKey}");
            GUI.Label(new Rect(0f, contentTop + 22f, contentRect.width, 20f), "Choose your live weapon loadout.");

            if (GUI.Button(new Rect(0f, contentTop + 50f, 96f, 24f), "Base Only"))
            {
                EquipBaseOnly();
            }

            if (GUI.Button(new Rect(106f, contentTop + 50f, 96f, 24f), "All Weapons"))
            {
                EquipAllWeapons();
            }

            if (GUI.Button(new Rect(212f, contentTop + 50f, 100f, 24f), "Refresh"))
            {
                RebuildWeaponCatalog();
                SyncSelectionFromLoadout();
                availableCharacterProfiles = RuntimeCharacterCatalog.GetCharacterProfiles();
                availableActiveDefinitions = RuntimeCharacterCatalog.GetSandboxActivePool();
            }

            GUI.Label(new Rect(0f, contentTop + 106f, contentRect.width, 18f), $"Character: {(currentProfile != null ? currentProfile.DisplayName : "Unknown")}");
            DrawCharacterPicker(contentTop + 128f, contentRect.width, currentProfile);

            var abilitySectionTop = contentTop + 128f + (characterRows * rowHeight) + 12f;
            GUI.Label(new Rect(0f, abilitySectionTop, contentRect.width, 18f), $"Primary Active: {(currentActive != null ? currentActive.DisplayName : "None")}");
            DrawAbilityPicker(abilitySectionTop + 22f, contentRect.width, currentActive);

            var controlsTop = abilitySectionTop + 22f + (abilityRows * rowHeight) + 12f;

            if (GUI.Button(new Rect(0f, controlsTop, 152f, 24f), "Upgrade A All"))
            {
                UpgradeAllActiveWeapons(WeaponUpgradePath.PathA);
            }

            if (GUI.Button(new Rect(160f, controlsTop, 152f, 24f), "Upgrade B All"))
            {
                UpgradeAllActiveWeapons(WeaponUpgradePath.PathB);
            }

            if (GUI.Button(new Rect(0f, controlsTop + 32f, contentRect.width, 24f), GetInvincibilityToggleLabel()))
            {
                TogglePlayerInvincibility();
            }

            GUI.Label(new Rect(0f, controlsTop + 64f, contentRect.width, 36f), $"Active: {string.Join(", ", combatLoadout.Weapons.Select(weapon => weapon.DisplayName))}");
            if (GUI.Button(new Rect(0f, controlsTop + 100f, contentRect.width, 24f), $"Increase Spawn Pressure ({spawnBoostCount})"))
            {
                IncreaseSpawnPressure();
            }

            if (GUI.Button(new Rect(0f, controlsTop + 130f, contentRect.width, 24f), GetAutoSpawnToggleLabel()))
            {
                ToggleAutoSpawning();
            }

            if (GUI.Button(new Rect(0f, controlsTop + 160f, contentRect.width, 24f), GetCorruptManualSpawnsLabel()))
            {
                corruptManualSpawns = !corruptManualSpawns;
            }

            GUI.Label(new Rect(0f, controlsTop + 190f, contentRect.width, 18f), $"Corruption: {currentCorruption:0}");

            if (GUI.Button(new Rect(0f, controlsTop + 212f, 152f, 24f), "+25 Corruption"))
            {
                AdjustCorruption(25f);
            }

            if (GUI.Button(new Rect(160f, controlsTop + 212f, 152f, 24f), "-25 Corruption"))
            {
                AdjustCorruption(-25f);
            }

            GUI.Label(new Rect(0f, controlsTop + 242f, contentRect.width, 18f), $"Manual enemy spawns: {manualSpawnCount}");

            if (GUI.Button(new Rect(0f, controlsTop + 264f, 152f, 24f), "Spawn Wretch"))
            {
                SpawnEnemy(SpawnEnemyKind.MireWretch);
            }

            if (GUI.Button(new Rect(160f, controlsTop + 264f, 152f, 24f), "Spawn Acolyte"))
            {
                SpawnEnemy(SpawnEnemyKind.DrownedAcolyte);
            }

            if (GUI.Button(new Rect(0f, controlsTop + 292f, 152f, 24f), "Spawn Mermaid"))
            {
                SpawnEnemy(SpawnEnemyKind.Mermaid);
            }

            if (GUI.Button(new Rect(160f, controlsTop + 292f, 152f, 24f), "Spawn Deep Spawn"))
            {
                SpawnEnemy(SpawnEnemyKind.DeepSpawn);
            }

            if (GUI.Button(new Rect(0f, controlsTop + 320f, 152f, 24f), "Spawn Eye"))
            {
                SpawnEnemy(SpawnEnemyKind.WatcherEye);
            }

            if (GUI.Button(new Rect(160f, controlsTop + 320f, 152f, 24f), "Spawn Slime"))
            {
                SpawnEnemy(SpawnEnemyKind.Slime);
            }

            if (GUI.Button(new Rect(0f, controlsTop + 348f, 152f, 24f), "Spawn Parasite"))
            {
                SpawnEnemy(SpawnEnemyKind.Parasite);
            }

            if (GUI.Button(new Rect(160f, controlsTop + 348f, 152f, 24f), "Spawn Mire Boss"))
            {
                SpawnBoss();
            }

            if (GUI.Button(new Rect(0f, controlsTop + 376f, 152f, 24f), "Spawn Monolith"))
            {
                SpawnMonolithBoss();
            }

            if (GUI.Button(new Rect(160f, controlsTop + 376f, 152f, 24f), "Spawn Admiral"))
            {
                SpawnAdmiralBoss();
            }

            GUI.Label(new Rect(0f, controlsTop + 408f, contentRect.width, 18f), "Corruption Events");

            if (GUI.Button(new Rect(0f, controlsTop + 430f, 152f, 24f), "Trigger Fodder"))
            {
                TriggerCorruptionEvent(CorruptionEventKind.Fodder);
            }

            if (GUI.Button(new Rect(160f, controlsTop + 430f, 152f, 24f), "Trigger Specialist"))
            {
                TriggerCorruptionEvent(CorruptionEventKind.Specialist);
            }

            if (GUI.Button(new Rect(0f, controlsTop + 458f, 152f, 24f), "Trigger Elite"))
            {
                TriggerCorruptionEvent(CorruptionEventKind.Elite);
            }

            if (GUI.Button(new Rect(160f, controlsTop + 458f, 152f, 24f), "Trigger Front"))
            {
                TriggerCorruptionEvent(CorruptionEventKind.Front);
            }

            if (GUI.Button(new Rect(0f, controlsTop + 490f, 152f, 24f), "Spawn Fountain"))
            {
                SpawnSandboxFountain();
            }

            if (GUI.Button(new Rect(160f, controlsTop + 490f, 152f, 24f), "Spawn Reliquary"))
            {
                SpawnSandboxReliquary();
            }

            var weaponListTop = controlsTop + 526f;

            for (var index = 0; index < availableWeapons.Count; index++)
            {
                DrawWeaponRow(index, availableWeapons[index], contentRect.width, weaponListTop);
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
            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<Health>();
            }
            if (corruptionEventDirector == null)
            {
                corruptionEventDirector = FindObjectOfType<CorruptionEventDirector>();
            }
            if (runStateManager == null)
            {
                runStateManager = FindObjectOfType<RunStateManager>();
            }
            if (corruptionMeter == null)
            {
                corruptionMeter = FindObjectOfType<CorruptionMeter>();
            }
            RebuildWeaponCatalog();
            SyncSelectionFromLoadout();
            availableCharacterProfiles = RuntimeCharacterCatalog.GetCharacterProfiles();
            availableActiveDefinitions = RuntimeCharacterCatalog.GetSandboxActivePool();
        }

        private string GetInvincibilityToggleLabel()
        {
            return playerInvincible ? "Invincibility: ON" : "Invincibility: OFF";
        }

        private string GetCorruptManualSpawnsLabel()
        {
            return corruptManualSpawns ? "Corrupt Manual Spawns: ON" : "Corrupt Manual Spawns: OFF";
        }

        private void TogglePlayerInvincibility()
        {
            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<Health>();
            }

            if (playerHealth == null)
            {
                return;
            }

            playerInvincible = !playerInvincible;
            var multiplier = playerInvincible ? 0f : 1f;
            playerHealth.SetIncomingDamageMultiplier(multiplier);
            playerHealth.SetIncomingContactDamageMultiplier(multiplier);
        }

        private void AdjustCorruption(float delta)
        {
            if (corruptionMeter == null)
            {
                corruptionMeter = FindObjectOfType<CorruptionMeter>();
            }

            if (corruptionMeter == null || Mathf.Approximately(delta, 0f))
            {
                return;
            }

            if (delta > 0f)
            {
                corruptionMeter.AddCorruption(delta);
            }
            else
            {
                corruptionMeter.ReduceCorruption(-delta);
            }
        }

        private static bool WasToggleKeyPressed(KeyCode keyCode)
        {
            if (Keyboard.current == null)
            {
                return false;
            }

            return keyCode switch
            {
                KeyCode.F1 => Keyboard.current.f1Key.wasPressedThisFrame,
                KeyCode.F2 => Keyboard.current.f2Key.wasPressedThisFrame,
                KeyCode.F3 => Keyboard.current.f3Key.wasPressedThisFrame,
                KeyCode.F4 => Keyboard.current.f4Key.wasPressedThisFrame,
                KeyCode.F5 => Keyboard.current.f5Key.wasPressedThisFrame,
                KeyCode.F6 => Keyboard.current.f6Key.wasPressedThisFrame,
                KeyCode.F7 => Keyboard.current.f7Key.wasPressedThisFrame,
                KeyCode.F8 => Keyboard.current.f8Key.wasPressedThisFrame,
                KeyCode.F9 => Keyboard.current.f9Key.wasPressedThisFrame,
                KeyCode.F10 => Keyboard.current.f10Key.wasPressedThisFrame,
                KeyCode.F11 => Keyboard.current.f11Key.wasPressedThisFrame,
                KeyCode.F12 => Keyboard.current.f12Key.wasPressedThisFrame,
                _ => false
            };
        }

        private void DrawWeaponRow(int index, WeaponDefinition definition, float width, float topOffset)
        {
            var top = topOffset + (index * 28f);
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

            GUI.enabled = runtimeWeapon != null && runtimeWeapon.CanUpgradePath(WeaponUpgradePath.PathA);
            if (GUI.Button(new Rect(width - 94f, top + 2f, 42f, 24f), "A+"))
            {
                runtimeWeapon?.ApplySandboxPathUpgrade(WeaponUpgradePath.PathA);
            }

            GUI.enabled = runtimeWeapon != null && runtimeWeapon.CanUpgradePath(WeaponUpgradePath.PathB);
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

            var eldritchBlast = RuntimeCharacterCatalog.GetCorruptionWeapon("weapon.eldritch_blast");
            if (eldritchBlast != null)
            {
                definitions.Add(eldritchBlast);
            }

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

        private void DrawCharacterPicker(float top, float width, CharacterProfileDefinition currentProfile)
        {
            const float buttonWidth = 152f;

            for (var index = 0; index < availableCharacterProfiles.Length; index++)
            {
                var profile = availableCharacterProfiles[index];
                if (profile == null)
                {
                    continue;
                }

                var column = index % 2;
                var row = index / 2;
                var x = column == 0 ? 0f : width - buttonWidth;
                var y = top + (row * 28f);
                var label = currentProfile == profile ? $"[{profile.DisplayName}]" : profile.DisplayName;
                if (GUI.Button(new Rect(x, y, buttonWidth, 24f), label))
                {
                    SwitchCharacter(profile);
                }
            }
        }

        private void DrawAbilityPicker(float top, float width, ActiveAbilityRuntime currentActive)
        {
            const float buttonWidth = 152f;

            for (var index = 0; index < availableActiveDefinitions.Length; index++)
            {
                var definition = availableActiveDefinitions[index];
                if (definition == null)
                {
                    continue;
                }

                var column = index % 2;
                var row = index / 2;
                var x = column == 0 ? 0f : width - buttonWidth;
                var y = top + (row * 28f);
                var isCurrent = currentActive != null && currentActive.AbilityId == definition.AbilityId;
                var label = isCurrent ? $"[{definition.DisplayName}]" : definition.DisplayName;
                if (GUI.Button(new Rect(x, y, buttonWidth, 24f), label))
                {
                    EquipPrimaryActive(definition);
                }
            }
        }

        private CharacterProfileDefinition ResolveCurrentCharacterProfile()
        {
            if (availableCharacterProfiles == null || availableCharacterProfiles.Length == 0)
            {
                return null;
            }

            var baseWeaponId = combatLoadout?.BaseWeapon?.WeaponId;
            var activeId = combatLoadout?.GetPrimaryActive()?.AbilityId;
            foreach (var profile in availableCharacterProfiles)
            {
                if (profile == null)
                {
                    continue;
                }

                var profileBaseWeaponId = profile.StartingBaseWeapon != null ? profile.StartingBaseWeapon.WeaponId : null;
                var profileActiveId = profile.StartingActive != null ? profile.StartingActive.AbilityId : null;
                if (profileBaseWeaponId == baseWeaponId && profileActiveId == activeId)
                {
                    return profile;
                }
            }

            return availableCharacterProfiles[0];
        }

        private void SwitchCharacter(CharacterProfileDefinition nextProfile)
        {
            if (nextProfile == null)
            {
                return;
            }

            RunSelectionState.SelectCharacter(nextProfile.CharacterId);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void EquipPrimaryActive(ActiveAbilityDefinition definition)
        {
            if (definition == null || combatLoadout == null)
            {
                return;
            }

            combatLoadout.EquipActive(0, definition);
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
                SpawnEnemyKind.MireWretch => spawnDirector.SpawnSandboxMireWretch(corruptManualSpawns),
                SpawnEnemyKind.DrownedAcolyte => spawnDirector.SpawnSandboxDrownedAcolyte(corruptManualSpawns),
                SpawnEnemyKind.Mermaid => spawnDirector.SpawnSandboxMermaid(corruptManualSpawns),
                SpawnEnemyKind.WatcherEye => spawnDirector.SpawnSandboxWatcherEye(corruptManualSpawns),
                SpawnEnemyKind.Slime => spawnDirector.SpawnSandboxSlime(corruptManualSpawns),
                SpawnEnemyKind.Parasite => spawnDirector.SpawnSandboxParasite(corruptManualSpawns),
                SpawnEnemyKind.DeepSpawn => spawnDirector.SpawnSandboxDeepSpawn(corruptManualSpawns),
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

        private void EnsureRunStateManager()
        {
            if (runStateManager == null)
            {
                runStateManager = FindObjectOfType<RunStateManager>();
            }
        }

        private void EnsureCorruptionEventDirector()
        {
            if (corruptionEventDirector == null)
            {
                corruptionEventDirector = FindObjectOfType<CorruptionEventDirector>();
            }
        }

        private void SpawnBoss()
        {
            EnsureRunStateManager();
            if (runStateManager == null)
            {
                return;
            }

            if (runStateManager.SpawnSandboxBoss(corruptManualSpawns))
            {
                manualSpawnCount += 1;
            }
        }

        private void SpawnMonolithBoss()
        {
            EnsureRunStateManager();
            if (runStateManager == null)
            {
                return;
            }

            if (runStateManager.SpawnSandboxMonolithBoss(corruptManualSpawns))
            {
                manualSpawnCount += 1;
            }
        }

        private void SpawnAdmiralBoss()
        {
            EnsureRunStateManager();
            if (runStateManager == null)
            {
                return;
            }

            if (runStateManager.SpawnSandboxAdmiralBoss(corruptManualSpawns))
            {
                manualSpawnCount += 1;
            }
        }

        private void TriggerCorruptionEvent(CorruptionEventKind eventKind)
        {
            EnsureCorruptionEventDirector();
            if (corruptionEventDirector == null)
            {
                return;
            }

            var triggered = eventKind switch
            {
                CorruptionEventKind.Fodder => corruptionEventDirector.TriggerFodderEvent(),
                CorruptionEventKind.Specialist => corruptionEventDirector.TriggerSpecialistEvent(),
                CorruptionEventKind.Elite => corruptionEventDirector.TriggerEliteEvent(),
                CorruptionEventKind.Front => corruptionEventDirector.TriggerCorruptionFront(),
                _ => false
            };

            if (triggered)
            {
                manualSpawnCount += 1;
            }
        }

        private void SpawnSandboxFountain()
        {
            var playerMover = FindObjectOfType<PlayerMover>();
            var meter = playerMover != null ? playerMover.GetComponent<CorruptionMeter>() : null;
            var cameraReference = Camera.main;
            if (playerMover == null || meter == null || cameraReference == null)
            {
                return;
            }

            var position = GetSandboxInteractablePosition(playerMover.transform, -1.1f);
            var fountain = CorruptionFountain.Create(
                position,
                25f,
                cameraReference,
                meter,
                new Vector2Int(int.MinValue + manualSpawnCount, int.MinValue),
                false,
                1f);
            fountain.transform.SetParent(transform, true);
            manualSpawnCount += 1;
        }

        private void SpawnSandboxReliquary()
        {
            var playerMover = FindObjectOfType<PlayerMover>();
            var cameraReference = Camera.main;
            if (playerMover == null || cameraReference == null || combatLoadout == null || playerHealth == null || corruptionMeter == null)
            {
                return;
            }

            var position = GetSandboxInteractablePosition(playerMover.transform, 1.1f);
            var reliquary = DrownedReliquary.Create(
                position,
                cameraReference,
                combatLoadout,
                playerHealth,
                corruptionMeter,
                new Vector2Int(int.MinValue + manualSpawnCount, int.MaxValue),
                false,
                1f);
            reliquary.transform.SetParent(transform, true);
            manualSpawnCount += 1;
        }

        private static Vector3 GetSandboxInteractablePosition(Transform playerTransform, float lateralOffset)
        {
            var forward = playerTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.01f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            var right = new Vector3(forward.z, 0f, -forward.x);
            return playerTransform.position + (forward * 3.4f) + (right * lateralOffset);
        }

        private enum SpawnEnemyKind
        {
            MireWretch,
            DrownedAcolyte,
            Mermaid,
            WatcherEye,
            Slime,
            Parasite,
            DeepSpawn
        }

        private enum CorruptionEventKind
        {
            Fodder,
            Specialist,
            Elite,
            Front
        }
    }
}
