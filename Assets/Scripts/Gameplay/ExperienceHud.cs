using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ExperienceHud : MonoBehaviour
    {
        private const int HeartsPerRow = 8;
        private static readonly Color XpFill = new(0.40f, 0.78f, 0.54f, 1f);
        private static readonly Color XpBackground = new(0.12f, 0.18f, 0.15f, 1f);
        private static readonly Color CorruptionFill = new(0.64f, 0.20f, 0.20f, 1f);
        private static readonly Color CorruptionBackground = new(0.17f, 0.09f, 0.09f, 1f);
        private static readonly Color CooldownReady = new(0.58f, 0.86f, 0.72f, 1f);
        private static readonly Color CooldownActive = new(0.23f, 0.55f, 0.46f, 1f);
        private static readonly Color CooldownBackground = new(0.10f, 0.16f, 0.16f, 1f);

        [SerializeField] private ExperienceController experienceController;
        [SerializeField] private Health playerHealth;
        [SerializeField] private CorruptionMeter corruptionMeter;
        [SerializeField] private PlayerCombatLoadout combatLoadout;
        [SerializeField] private CorruptionRuntimeEffects corruptionEffects;
        private Texture2D heartTexture;
        private Texture2D whiteTexture;
        private GUIStyle centeredTitleStyle;
        private GUIStyle centeredBodyStyle;
        private GUIStyle upgradeButtonStyle;
        private GUIStyle corruptionColumnStyle;
        private int selectedCorruptionBoonIndex = -1;
        private int selectedCorruptionDrawbackIndex = -1;
        private int activeCorruptionChoiceStage = -1;
        private int selectedReliquaryWeaponIndex = -1;

        private void Awake()
        {
            if (experienceController == null)
            {
                experienceController = FindObjectOfType<ExperienceController>();
            }

            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<PlayerMover>()?.GetComponent<Health>();
            }

            if (corruptionMeter == null)
            {
                corruptionMeter = FindObjectOfType<PlayerMover>()?.GetComponent<CorruptionMeter>();
            }

            if (combatLoadout == null)
            {
                combatLoadout = FindObjectOfType<PlayerCombatLoadout>();
            }

            if (corruptionEffects == null)
            {
                corruptionEffects = FindObjectOfType<CorruptionRuntimeEffects>();
            }

            heartTexture = Resources.Load<Texture2D>("Sprites/UI/heart");
            whiteTexture = Texture2D.whiteTexture;

            if (heartTexture != null)
            {
                heartTexture.filterMode = FilterMode.Point;
                heartTexture.wrapMode = TextureWrapMode.Clamp;
            }
        }

        private void OnGUI()
        {
            if (experienceController == null)
            {
                return;
            }

            EnsureStyles();

            DrawHealthDisplay();
            DrawExperienceBar();
            DrawCooldownPanel();
            DrawCorruptionBar();
            DrawWeaponStrip();

            if (DrownedReliquary.HasActiveInteraction)
            {
                Time.timeScale = 0f;
                DrawReliquaryOverlay(DrownedReliquary.GetInteractionView());
                return;
            }

            if (corruptionEffects != null && corruptionEffects.HasPendingChoice)
            {
                Time.timeScale = 0f;
                DrawCorruptionOverlay(corruptionEffects.PeekPendingChoice());
                return;
            }

            if (!experienceController.HasPendingChoice)
            {
                return;
            }

            var choices = experienceController.PeekChoices();
            Time.timeScale = 0f;
            DrawUpgradeOverlay(choices);
        }

        private static string DescribeChoice(CombatRewardOption choice)
        {
            return $"{choice.Title}\n{choice.Description}";
        }

        private void DrawHealthDisplay()
        {
            const float margin = 16f;
            var currentHealth = Mathf.CeilToInt(playerHealth?.CurrentHealth ?? 0f);
            var maxHealth = Mathf.CeilToInt(playerHealth?.MaxHealth ?? 0f);
            GUI.Label(new Rect(margin, margin, 180f, 22f), $"Health {currentHealth}/{maxHealth}");

            if (heartTexture == null || maxHealth <= 0)
            {
                return;
            }

            const float iconSize = 22f;
            const float spacing = 4f;
            var baseX = margin;
            var baseY = margin + 22f;

            for (var i = 0; i < maxHealth; i++)
            {
                var row = i / HeartsPerRow;
                var column = i % HeartsPerRow;
                var rect = new Rect(baseX + column * (iconSize + spacing), baseY + row * (iconSize + 2f), iconSize, iconSize);
                var color = i < currentHealth ? Color.white : new Color(1f, 1f, 1f, 0.18f);
                var previous = GUI.color;
                GUI.color = color;
                GUI.DrawTexture(rect, heartTexture, ScaleMode.ScaleToFit, true);
                GUI.color = previous;
            }
        }

        private void DrawExperienceBar()
        {
            const float barWidth = 240f;
            const float barHeight = 10f;
            const float topMargin = 20f;

            var progress = experienceController.RequiredXp > 0
                ? Mathf.Clamp01(experienceController.CurrentXp / (float)experienceController.RequiredXp)
                : 0f;
            var barX = (Screen.width - barWidth) * 0.5f;
            var barY = topMargin;

            GUI.Label(new Rect(barX, barY - 16f, 180f, 18f), $"Level {experienceController.Level}");
            DrawMeter(new Rect(barX, barY, barWidth, barHeight), progress, XpFill, XpBackground);
            GUI.Label(new Rect(barX + barWidth + 8f, barY - 4f, 96f, 18f), $"{experienceController.CurrentXp}/{experienceController.RequiredXp}");
        }

        private void DrawCooldownPanel()
        {
            const float margin = 16f;
            const float panelWidth = 156f;
            const float panelHeight = 36f;

            var activeAbility = combatLoadout != null ? combatLoadout.GetActive(0) : null;
            var cooldownRemaining = activeAbility != null ? activeAbility.CooldownRemaining : 0f;
            var cooldownDuration = activeAbility != null ? Mathf.Max(0.01f, activeAbility.CooldownDuration) : 1f;
            var ready = cooldownRemaining <= 0.01f;
            var progress = ready ? 1f : 1f - Mathf.Clamp01(cooldownRemaining / cooldownDuration);
            var panelX = Screen.width - panelWidth - margin;
            var panelY = Screen.height - panelHeight - margin;

            GUI.Label(new Rect(panelX, panelY - 2f, panelWidth - 44f, 18f), activeAbility != null ? activeAbility.DisplayName : "Active");
            DrawMeter(
                new Rect(panelX, panelY + 16f, 108f, 12f),
                progress,
                ready ? CooldownReady : CooldownActive,
                CooldownBackground);
            GUI.Label(
                new Rect(panelX + 116f, panelY + 12f, 40f, 18f),
                ready ? "Ready" : $"{cooldownRemaining:0.0}s");
        }

        private void DrawWeaponStrip()
        {
            if (combatLoadout == null || combatLoadout.Weapons.Count == 0)
            {
                return;
            }

            const float margin = 16f;
            var y = Screen.height - 92f;
            GUI.Label(new Rect(margin, y, 280f, 18f), "Weapons");

            for (var i = 0; i < combatLoadout.Weapons.Count; i++)
            {
                var weapon = combatLoadout.Weapons[i];
                var label = weapon.IsBaseWeapon
                    ? $"{weapon.DisplayName} [Base] R{weapon.Rank}"
                    : $"{weapon.DisplayName} R{weapon.Rank}";
                GUI.Label(new Rect(margin, y + 18f + (i * 18f), 320f, 18f), label);
            }
        }

        private void DrawCorruptionBar()
        {
            const float barWidth = 280f;
            const float barHeight = 12f;
            const float topMargin = 44f;

            var currentCorruption = corruptionMeter?.CurrentCorruption ?? 0f;
            var meterThresholds = corruptionMeter != null ? corruptionMeter.ThresholdValues : null;
            var displayMaxCorruption = meterThresholds != null && meterThresholds.Length > 0
                ? meterThresholds[meterThresholds.Length - 1]
                : (corruptionMeter?.MaxCorruption ?? 0f);
            var progress = displayMaxCorruption > 0f
                ? Mathf.Clamp01(currentCorruption / displayMaxCorruption)
                : 0f;
            var stage = corruptionMeter != null ? corruptionMeter.CurrentStageIndex + 1 : 0;
            var barX = (Screen.width - barWidth) * 0.5f;
            var barY = topMargin;

            GUI.Label(new Rect(barX, barY - 18f, 180f, 18f), stage > 0 ? $"Corruption T{stage}" : "Corruption");
            DrawMeter(new Rect(barX, barY, barWidth, barHeight), progress, CorruptionFill, CorruptionBackground);
            if (corruptionMeter != null)
            {
                var thresholds = corruptionMeter.ThresholdValues;
                if (thresholds != null)
                {
                    for (var i = 0; i < thresholds.Length; i++)
                    {
                        var markerProgress = displayMaxCorruption > 0f ? thresholds[i] / displayMaxCorruption : 0f;
                        var markerX = barX + (barWidth * markerProgress);
                        GUI.DrawTexture(new Rect(markerX - 1f, barY - 2f, 2f, barHeight + 4f), whiteTexture, ScaleMode.StretchToFill, false);
                    }
                }
            }
            var corruptionLabel = displayMaxCorruption > 0f && currentCorruption > displayMaxCorruption
                ? $"{currentCorruption:0}+"
                : $"{currentCorruption:0}/{displayMaxCorruption:0}";
            GUI.Label(new Rect(barX + barWidth + 8f, barY - 4f, 96f, 18f), corruptionLabel);
        }

        private void DrawMeter(Rect rect, float progress, Color fillColor, Color backgroundColor)
        {
            if (whiteTexture == null)
            {
                return;
            }

            var previous = GUI.color;
            var inner = rect;
            GUI.color = backgroundColor;
            GUI.DrawTexture(inner, whiteTexture, ScaleMode.StretchToFill, false);

            if (progress > 0f)
            {
                GUI.color = fillColor;
                GUI.DrawTexture(new Rect(inner.x, inner.y, inner.width * Mathf.Clamp01(progress), inner.height), whiteTexture, ScaleMode.StretchToFill, false);
            }

            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (centeredTitleStyle != null && centeredBodyStyle != null && upgradeButtonStyle != null && corruptionColumnStyle != null)
            {
                return;
            }

            centeredTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            centeredTitleStyle.normal.textColor = new Color(0.92f, 0.96f, 0.90f, 1f);

            centeredBodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                wordWrap = true
            };
            centeredBodyStyle.normal.textColor = new Color(0.70f, 0.82f, 0.76f, 1f);

            upgradeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                padding = new RectOffset(18, 18, 16, 16),
                margin = new RectOffset(0, 0, 0, 0)
            };
            upgradeButtonStyle.normal.background = whiteTexture;
            upgradeButtonStyle.hover.background = whiteTexture;
            upgradeButtonStyle.active.background = whiteTexture;
            upgradeButtonStyle.focused.background = whiteTexture;
            upgradeButtonStyle.normal.textColor = new Color(0.93f, 0.98f, 0.94f, 1f);
            upgradeButtonStyle.hover.textColor = Color.white;
            upgradeButtonStyle.active.textColor = Color.white;

            corruptionColumnStyle = new GUIStyle(centeredBodyStyle)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 14
            };
        }

        private void DrawUpgradeOverlay(CombatRewardChoiceSet choices)
        {
            if (whiteTexture == null)
            {
                return;
            }

            var previousColor = GUI.color;

            GUI.color = new Color(0.02f, 0.04f, 0.04f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture, ScaleMode.StretchToFill, false);

            var scale = Mathf.Max(1.1f, Mathf.Min(Screen.width / 1400f, Screen.height / 900f) * 1.35f);
            var panelWidth = 760f;
            var panelHeight = 420f;
            var scaledWidth = Screen.width / scale;
            var scaledHeight = Screen.height / scale;
            var panelX = (scaledWidth - panelWidth) * 0.5f;
            var panelY = (scaledHeight - panelHeight) * 0.5f;

            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            var panelRect = new Rect(panelX, panelY, panelWidth, panelHeight);

            GUI.color = new Color(0.08f, 0.16f, 0.14f, 0.92f);
            GUI.DrawTexture(panelRect, whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = new Color(0.25f, 0.48f, 0.38f, 0.95f);
            GUI.DrawTexture(new Rect(panelRect.x + 4f, panelRect.y + 4f, panelRect.width - 8f, 6f), whiteTexture, ScaleMode.StretchToFill, false);

            GUI.color = previousColor;
            GUI.Label(new Rect(panelRect.x + 40f, panelRect.y + 30f, panelRect.width - 80f, 34f), "Choose Upgrade", centeredTitleStyle);
            GUI.Label(
                new Rect(panelRect.x + 70f, panelRect.y + 72f, panelRect.width - 140f, 42f),
                "Pick 1",
                centeredBodyStyle);

            var buttonWidth = panelRect.width - 120f;
            var buttonHeight = 72f;
            var buttonX = panelRect.x + 60f;
            var buttonStartY = panelRect.y + 136f;
            var buttonGap = 20f;

            for (var i = 0; i < choices.Options.Length; i++)
            {
                var buttonRect = new Rect(buttonX, buttonStartY + (i * (buttonHeight + buttonGap)), buttonWidth, buttonHeight);
                DrawUpgradeButton(buttonRect);
                GUI.backgroundColor = new Color(1f, 1f, 1f, 0.02f);
                if (GUI.Button(buttonRect, DescribeChoice(choices.Options[i]), upgradeButtonStyle))
                {
                    Time.timeScale = 1f;
                    experienceController.ApplyChoice(i);
                }
            }

            GUI.matrix = previousMatrix;
            GUI.backgroundColor = Color.white;
            GUI.color = previousColor;
        }

        private void DrawCorruptionOverlay(CorruptionRuntimeEffects.CorruptionChoiceView choice)
        {
            if (whiteTexture == null)
            {
                return;
            }

            if (activeCorruptionChoiceStage != choice.StageIndex)
            {
                activeCorruptionChoiceStage = choice.StageIndex;
                selectedCorruptionBoonIndex = -1;
                selectedCorruptionDrawbackIndex = -1;
            }

            var previousColor = GUI.color;
            GUI.color = new Color(0.02f, 0.04f, 0.04f, 0.58f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture, ScaleMode.StretchToFill, false);

            var scale = Mathf.Max(1.05f, Mathf.Min(Screen.width / 1450f, Screen.height / 920f) * 1.22f);
            var panelWidth = 860f;
            var panelHeight = 430f;
            var scaledWidth = Screen.width / scale;
            var scaledHeight = Screen.height / scale;
            var panelX = (scaledWidth - panelWidth) * 0.5f;
            var panelY = (scaledHeight - panelHeight) * 0.5f;

            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            var panelRect = new Rect(panelX, panelY, panelWidth, panelHeight);
            GUI.color = new Color(0.08f, 0.13f, 0.12f, 0.94f);
            GUI.DrawTexture(panelRect, whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = new Color(0.29f, 0.44f, 0.36f, 0.95f);
            GUI.DrawTexture(new Rect(panelRect.x + 4f, panelRect.y + 4f, panelRect.width - 8f, 6f), whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = previousColor;

            GUI.Label(new Rect(panelRect.x + 40f, panelRect.y + 26f, panelRect.width - 80f, 34f), $"Corruption Stage {choice.StageIndex + 1}", centeredTitleStyle);
            var subtitle = choice.RequiresBoonSelection
                ? $"Crossed {choice.ThresholdValue:0} corruption. Choose 1 boon. The burden is forced."
                : $"Crossed {choice.ThresholdValue:0} corruption. Choose 1 catastrophic drawback.";
            GUI.Label(new Rect(panelRect.x + 40f, panelRect.y + 62f, panelRect.width - 80f, 28f), subtitle, centeredBodyStyle);

            var gutter = 26f;
            var columnHeight = 238f;
            if (choice.RequiresBoonSelection)
            {
                var columnWidth = (panelRect.width - (gutter * 3f)) * 0.5f;
                var boonRect = new Rect(panelRect.x + gutter, panelRect.y + 108f, columnWidth, columnHeight);
                var drawbackRect = new Rect(boonRect.xMax + gutter, boonRect.y, columnWidth, columnHeight);

                DrawCorruptionChoiceColumn(boonRect, "Boon", choice.Boons, true, ref selectedCorruptionBoonIndex, new Color(0.11f, 0.19f, 0.16f, 0.94f));
                DrawForcedCorruptionDrawback(drawbackRect, "Burden", choice.Drawbacks, new Color(0.12f, 0.15f, 0.14f, 0.94f));
                selectedCorruptionDrawbackIndex = choice.Drawbacks.Length > 0 ? 0 : -1;
            }
            else
            {
                var singleRect = new Rect(panelRect.x + 120f, panelRect.y + 108f, panelRect.width - 240f, columnHeight);
                DrawCorruptionChoiceColumn(singleRect, "Catastrophe", choice.Drawbacks, false, ref selectedCorruptionDrawbackIndex, new Color(0.12f, 0.15f, 0.14f, 0.94f));
                selectedCorruptionBoonIndex = -1;
            }

            var confirmRect = new Rect(panelRect.x + 250f, panelRect.yMax - 70f, panelRect.width - 500f, 42f);
            DrawUpgradeButton(confirmRect);
            var ready = selectedCorruptionDrawbackIndex >= 0 && (!choice.RequiresBoonSelection || selectedCorruptionBoonIndex >= 0);
            GUI.enabled = ready;
            if (GUI.Button(confirmRect, "Accept", upgradeButtonStyle))
            {
                corruptionEffects?.ApplyPendingChoice(selectedCorruptionBoonIndex, selectedCorruptionDrawbackIndex);
                selectedCorruptionBoonIndex = -1;
                selectedCorruptionDrawbackIndex = -1;
                activeCorruptionChoiceStage = -1;
                Time.timeScale = 1f;
            }

            GUI.enabled = true;
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void DrawCorruptionChoiceColumn(Rect rect, string title, CorruptionRuntimeEffects.CorruptionOptionView[] options, bool isBoon, ref int selectedIndex, Color backgroundColor)
        {
            GUI.color = backgroundColor;
            GUI.DrawTexture(rect, whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 20f, rect.y + 12f, rect.width - 40f, 28f), title, centeredTitleStyle);

            for (var i = 0; i < options.Length; i++)
            {
                var optionRect = new Rect(rect.x + 14f, rect.y + 48f + (i * 58f), rect.width - 28f, 50f);
                var selected = selectedIndex == i;
                var option = options[i];
                var special = option.IsCorruptionActive;
                GUI.color = selected
                    ? (special
                        ? new Color(0.42f, 0.15f, 0.17f, 0.98f)
                        : (isBoon ? new Color(0.22f, 0.39f, 0.30f, 0.97f) : new Color(0.23f, 0.28f, 0.24f, 0.97f)))
                    : (special ? new Color(0.92f, 0.20f, 0.24f, 0.16f) : new Color(1f, 1f, 1f, 0.05f));
                GUI.DrawTexture(optionRect, whiteTexture, ScaleMode.StretchToFill, false);
                GUI.color = Color.white;

                if (GUI.Button(optionRect, GUIContent.none, GUIStyle.none))
                {
                    selectedIndex = i;
                }

                GUI.Label(new Rect(optionRect.x + 12f, optionRect.y + 5f, optionRect.width - 24f, 18f), option.Title, centeredBodyStyle);
                GUI.Label(new Rect(optionRect.x + 12f, optionRect.y + 22f, optionRect.width - 24f, 20f), option.Description, corruptionColumnStyle);
            }
        }

        private void DrawForcedCorruptionDrawback(Rect rect, string title, CorruptionRuntimeEffects.CorruptionOptionView[] options, Color backgroundColor)
        {
            GUI.color = backgroundColor;
            GUI.DrawTexture(rect, whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 20f, rect.y + 12f, rect.width - 40f, 28f), title, centeredTitleStyle);

            if (options == null || options.Length <= 0)
            {
                return;
            }

            var option = options[0];
            var optionRect = new Rect(rect.x + 14f, rect.y + 62f, rect.width - 28f, 94f);
            GUI.color = new Color(0.23f, 0.28f, 0.24f, 0.97f);
            GUI.DrawTexture(optionRect, whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;
            GUI.Label(new Rect(optionRect.x + 12f, optionRect.y + 7f, optionRect.width - 24f, 18f), option.Title, centeredBodyStyle);
            GUI.Label(new Rect(optionRect.x + 12f, optionRect.y + 28f, optionRect.width - 24f, 32f), option.Description, corruptionColumnStyle);
        }

        private void DrawUpgradeButton(Rect rect)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.11f, 0.22f, 0.18f, 0.98f);
            GUI.DrawTexture(rect, whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = new Color(0.33f, 0.62f, 0.47f, 0.9f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 8f, rect.height), whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = new Color(1f, 1f, 1f, 0.06f);
            GUI.DrawTexture(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f), whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = previousColor;
        }

        private void DrawReliquaryOverlay(DrownedReliquary.InteractionView view)
        {
            if (whiteTexture == null)
            {
                return;
            }

            if (selectedReliquaryWeaponIndex >= view.RemovableWeapons.Length)
            {
                selectedReliquaryWeaponIndex = view.RemovableWeapons.Length > 0 ? 0 : -1;
            }

            var previousColor = GUI.color;
            GUI.color = new Color(0.01f, 0.03f, 0.03f, 0.70f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture, ScaleMode.StretchToFill, false);

            var scale = Mathf.Max(1.05f, Mathf.Min(Screen.width / 1420f, Screen.height / 900f) * 1.2f);
            var panelWidth = 860f;
            var panelHeight = 470f;
            var scaledWidth = Screen.width / scale;
            var scaledHeight = Screen.height / scale;
            var panelX = (scaledWidth - panelWidth) * 0.5f;
            var panelY = (scaledHeight - panelHeight) * 0.5f;

            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            var panelRect = new Rect(panelX, panelY, panelWidth, panelHeight);
            GUI.color = new Color(0.10f, 0.09f, 0.10f, 0.95f);
            GUI.DrawTexture(panelRect, whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = new Color(0.46f, 0.14f, 0.14f, 0.92f);
            GUI.DrawTexture(new Rect(panelRect.x + 4f, panelRect.y + 4f, panelRect.width - 8f, 6f), whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = previousColor;

            GUI.Label(new Rect(panelRect.x + 40f, panelRect.y + 28f, panelRect.width - 80f, 34f), view.Title, centeredTitleStyle);
            GUI.Label(new Rect(panelRect.x + 60f, panelRect.y + 68f, panelRect.width - 120f, 30f), view.Subtitle, centeredBodyStyle);

            var listRect = new Rect(panelRect.x + 36f, panelRect.y + 118f, panelRect.width - 72f, 178f);
            GUI.color = new Color(0.16f, 0.14f, 0.15f, 0.96f);
            GUI.DrawTexture(listRect, whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;

            if (view.RemovableWeapons.Length <= 0)
            {
                GUI.Label(new Rect(listRect.x + 20f, listRect.y + 68f, listRect.width - 40f, 32f), "No non-base weapon can be sacrificed.", centeredBodyStyle);
            }
            else
            {
                for (var i = 0; i < view.RemovableWeapons.Length; i++)
                {
                    var option = view.RemovableWeapons[i];
                    var rowRect = new Rect(listRect.x + 16f, listRect.y + 14f + (i * 50f), listRect.width - 32f, 42f);
                    var selected = selectedReliquaryWeaponIndex == i || (selectedReliquaryWeaponIndex < 0 && i == 0);
                    GUI.color = selected ? new Color(0.40f, 0.16f, 0.16f, 0.98f) : new Color(1f, 1f, 1f, 0.06f);
                    GUI.DrawTexture(rowRect, whiteTexture, ScaleMode.StretchToFill, false);
                    GUI.color = Color.white;

                    if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
                    {
                        selectedReliquaryWeaponIndex = i;
                    }

                    GUI.Label(new Rect(rowRect.x + 12f, rowRect.y + 4f, rowRect.width - 24f, 16f), option.DisplayName, centeredBodyStyle);
                    GUI.Label(new Rect(rowRect.x + 12f, rowRect.y + 20f, rowRect.width - 24f, 18f), option.Description, corruptionColumnStyle);
                }
            }

            var canConfirm = view.RemovableWeapons.Length > 0;
            var resolvedWeaponIndex = selectedReliquaryWeaponIndex >= 0 ? selectedReliquaryWeaponIndex : 0;
            var selectedWeapon = canConfirm && resolvedWeaponIndex < view.RemovableWeapons.Length ? view.RemovableWeapons[resolvedWeaponIndex] : null;

            var corruptionRect = new Rect(panelRect.x + 74f, panelRect.y + 332f, panelRect.width - 148f, 42f);
            DrawUpgradeButton(corruptionRect);
            GUI.enabled = canConfirm && selectedWeapon != null;
            if (GUI.Button(corruptionRect, "Cast Into The Deep   (+25 Corruption)", upgradeButtonStyle))
            {
                DrownedReliquary.ConfirmCorruptionCost(selectedWeapon.WeaponId);
                selectedReliquaryWeaponIndex = -1;
                Time.timeScale = 1f;
            }

            var healthRect = new Rect(panelRect.x + 74f, panelRect.y + 384f, panelRect.width - 148f, 42f);
            DrawUpgradeButton(healthRect);
            if (GUI.Button(healthRect, "Bleed For Release   (-4 Max Health)", upgradeButtonStyle))
            {
                DrownedReliquary.ConfirmMaxHealthCost(selectedWeapon.WeaponId);
                selectedReliquaryWeaponIndex = -1;
                Time.timeScale = 1f;
            }

            GUI.enabled = true;
            var cancelRect = new Rect(panelRect.x + 280f, panelRect.y + 432f, panelRect.width - 560f, 28f);
            if (GUI.Button(cancelRect, "Cancel"))
            {
                DrownedReliquary.CancelInteraction();
                selectedReliquaryWeaponIndex = -1;
                Time.timeScale = 1f;
            }

            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }
    }
}
