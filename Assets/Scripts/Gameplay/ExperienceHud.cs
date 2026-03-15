using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ExperienceHud : MonoBehaviour
    {
        [SerializeField] private ExperienceController experienceController;
        [SerializeField] private Health playerHealth;
        [SerializeField] private CorruptionMeter corruptionMeter;
        [SerializeField] private BrineSurgeAbility brineSurgeAbility;

        private readonly Rect hudRect = new(16f, 16f, 320f, 140f);
        private readonly Rect choiceRect = new(16f, 170f, 420f, 180f);

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
        }

        private void OnGUI()
        {
            if (experienceController == null)
            {
                return;
            }

            GUI.Box(hudRect, "Dagon");
            GUI.Label(new Rect(28f, 44f, 260f, 22f), $"Health: {playerHealth?.CurrentHealth ?? 0:0}/{playerHealth?.MaxHealth ?? 0:0}");
            GUI.Label(new Rect(28f, 66f, 260f, 22f), $"Level: {experienceController.Level}");
            GUI.Label(new Rect(28f, 88f, 260f, 22f), $"XP: {experienceController.CurrentXp}/{experienceController.RequiredXp}");
            GUI.Label(new Rect(28f, 110f, 260f, 22f), $"Corruption: {corruptionMeter?.CurrentCorruption ?? 0:0}/{corruptionMeter?.MaxCorruption ?? 0:0}");
            GUI.Label(new Rect(180f, 66f, 160f, 22f), $"Brine: {(brineSurgeAbility != null ? brineSurgeAbility.CooldownRemaining : 0f):0.0}s");

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
    }
}
