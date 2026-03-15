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
        [SerializeField] private BrineSurgeAbility brineSurgeAbility;

        private Texture2D heartTexture;
        private Texture2D whiteTexture;
        private GUIStyle centeredTitleStyle;
        private GUIStyle centeredBodyStyle;
        private GUIStyle upgradeButtonStyle;

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

            if (brineSurgeAbility == null)
            {
                brineSurgeAbility = FindObjectOfType<BrineSurgeAbility>();
            }

            heartTexture = Resources.Load<Texture2D>("Sprites/UI/heart");
            whiteTexture = Texture2D.whiteTexture;
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

            if (!experienceController.HasPendingChoice)
            {
                if (Time.timeScale == 0f)
                {
                    Time.timeScale = 1f;
                }
                return;
            }

            var choices = experienceController.PeekChoices();
            Time.timeScale = 0f;
            DrawUpgradeOverlay(choices);
        }

        private static string DescribeChoice(UpgradeChoice choice)
        {
            return choice switch
            {
                UpgradeChoice.AttackRate => "Tighter Rhythm: increase harpoon fire rate",
                UpgradeChoice.ProjectileDamage => "Barbed Iron: increase harpoon damage",
                UpgradeChoice.ProjectileCount => "Split Cast: fire one additional harpoon",
                UpgradeChoice.BrineRadius => "Rising Tide: increase Brine Surge radius",
                UpgradeChoice.MaxHealth => "Salt-Hardened: increase max health",
                UpgradeChoice.CorruptionPulse => "Tide of Dagon: gain corruption and power",
                _ => choice.ToString()
            };
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
            const float barHeight = 18f;
            const float topMargin = 18f;

            var progress = experienceController.RequiredXp > 0
                ? Mathf.Clamp01(experienceController.CurrentXp / (float)experienceController.RequiredXp)
                : 0f;
            var barX = (Screen.width - barWidth) * 0.5f;
            var barY = topMargin;

            GUI.Label(new Rect(barX, barY - 16f, 180f, 18f), $"Level {experienceController.Level}");
            DrawMeter(new Rect(barX, barY, barWidth, barHeight), progress, XpFill, XpBackground);
            GUI.Label(new Rect(barX + 6f, barY + 1f, barWidth - 12f, 18f), $"XP {experienceController.CurrentXp}/{experienceController.RequiredXp}");
        }

        private void DrawCooldownPanel()
        {
            const float margin = 16f;
            const float panelWidth = 156f;
            const float panelHeight = 36f;

            var cooldownRemaining = brineSurgeAbility != null ? brineSurgeAbility.CooldownRemaining : 0f;
            var cooldownDuration = brineSurgeAbility != null ? Mathf.Max(0.01f, brineSurgeAbility.CooldownDuration) : 1f;
            var ready = cooldownRemaining <= 0.01f;
            var progress = ready ? 1f : 1f - Mathf.Clamp01(cooldownRemaining / cooldownDuration);
            var panelX = Screen.width - panelWidth - margin;
            var panelY = Screen.height - panelHeight - margin;

            GUI.Label(new Rect(panelX, panelY - 2f, panelWidth - 44f, 18f), "Brine Surge");
            DrawMeter(
                new Rect(panelX, panelY + 16f, 108f, 12f),
                progress,
                ready ? CooldownReady : CooldownActive,
                CooldownBackground);
            GUI.Label(
                new Rect(panelX + 116f, panelY + 12f, 40f, 18f),
                ready ? "Ready" : $"{cooldownRemaining:0.0}s");
        }

        private void DrawCorruptionBar()
        {
            const float margin = 16f;
            const float barWidth = 240f;
            const float barHeight = 12f;

            var currentCorruption = corruptionMeter?.CurrentCorruption ?? 0f;
            var maxCorruption = corruptionMeter?.MaxCorruption ?? 0f;
            var progress = maxCorruption > 0f ? Mathf.Clamp01(currentCorruption / maxCorruption) : 0f;
            var barX = margin;
            var barY = Screen.height - 28f;

            GUI.Label(new Rect(barX, barY - 18f, 180f, 18f), "Corruption");
            DrawMeter(new Rect(barX, barY, barWidth, barHeight), progress, CorruptionFill, CorruptionBackground);
            GUI.Label(new Rect(barX + barWidth - 72f, barY - 18f, 72f, 18f), $"{currentCorruption:0}/{maxCorruption:0}");
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
            if (centeredTitleStyle != null && centeredBodyStyle != null && upgradeButtonStyle != null)
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
        }

        private void DrawUpgradeOverlay(UpgradeChoiceSet choices)
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
            GUI.Label(new Rect(panelRect.x + 40f, panelRect.y + 30f, panelRect.width - 80f, 34f), "Choose an Upgrade", centeredTitleStyle);
            GUI.Label(
                new Rect(panelRect.x + 70f, panelRect.y + 72f, panelRect.width - 140f, 42f),
                "The run is paused. Pick one boon before the mire closes in again.",
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
    }
}
