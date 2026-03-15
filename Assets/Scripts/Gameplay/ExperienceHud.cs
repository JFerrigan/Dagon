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

        private readonly Rect choiceRect = new(16f, 170f, 420f, 180f);
        private Texture2D heartTexture;
        private Texture2D whiteTexture;

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
            GUI.Box(choiceRect, "Choose an Upgrade");

            Time.timeScale = 0f;
            for (var i = 0; i < choices.Options.Length; i++)
            {
                var buttonRect = new Rect(32f, 210f + (i * 42f), 380f, 30f);
                if (GUI.Button(buttonRect, DescribeChoice(choices.Options[i])))
                {
                    Time.timeScale = 1f;
                    experienceController.ApplyChoice(i);
                }
            }
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
    }
}
