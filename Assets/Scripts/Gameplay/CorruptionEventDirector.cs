using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CorruptionEventDirector : MonoBehaviour
    {
        private enum CorruptionEventKind
        {
            RottingSwell,
            SirenPressure,
            DeepTide,
            CorruptionFront
        }

        private const float FrontPressureDuration = 12f;
        private const float FrontPressureWaveInterval = 3f;
        private static readonly Color BannerColor = new(0.84f, 0.30f, 0.30f, 1f);

        [SerializeField] private CorruptionMeter corruptionMeter;
        [SerializeField] private SpawnDirector spawnDirector;
        [SerializeField] private RunStateManager runStateManager;

        private float eventTimer;
        private float bannerTimer;
        private float frontPressureRemaining;
        private float frontPressureWaveTimer;
        private string activeBannerTitle = string.Empty;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;

        public float EventTimerRemaining => Mathf.Max(0f, eventTimer);

        private void Start()
        {
            ResetEventTimer();
        }

        private void Update()
        {
            bannerTimer = Mathf.Max(0f, bannerTimer - Time.deltaTime);

            if (Time.timeScale <= 0f)
            {
                return;
            }

            TickFrontPressure(Time.deltaTime);

            if (corruptionMeter == null || spawnDirector == null)
            {
                return;
            }

            if (runStateManager != null && runStateManager.BossWaveStarted)
            {
                return;
            }

            if (corruptionMeter.CurrentStageIndex < 0)
            {
                return;
            }

            eventTimer -= Time.deltaTime;
            if (eventTimer > 0f)
            {
                return;
            }

            TriggerAutomaticEvent();
        }

        private void OnGUI()
        {
            if (bannerTimer <= 0f)
            {
                return;
            }

            EnsureStyles();
            var alpha = Mathf.Clamp01(bannerTimer / 3f);
            var previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect((Screen.width - 360f) * 0.5f, 82f, 360f, 22f), "Corruption Event", titleStyle);
            GUI.color = new Color(BannerColor.r, BannerColor.g, BannerColor.b, alpha);
            GUI.Label(new Rect((Screen.width - 420f) * 0.5f, 104f, 420f, 22f), activeBannerTitle, bodyStyle);
            GUI.color = previous;
        }

        public void Configure(CorruptionMeter meter, SpawnDirector director, RunStateManager runState)
        {
            corruptionMeter = meter;
            spawnDirector = director;
            runStateManager = runState;
            ResetEventTimer();
        }

        public bool TriggerFodderEvent()
        {
            return TriggerEvent(CorruptionEventKind.RottingSwell, isManual: true);
        }

        public bool TriggerSpecialistEvent()
        {
            return TriggerEvent(CorruptionEventKind.SirenPressure, isManual: true);
        }

        public bool TriggerEliteEvent()
        {
            return TriggerEvent(CorruptionEventKind.DeepTide, isManual: true);
        }

        public bool TriggerCorruptionFront()
        {
            return TriggerEvent(CorruptionEventKind.CorruptionFront, isManual: true);
        }

        private void TriggerAutomaticEvent()
        {
            var stageIndex = corruptionMeter != null ? corruptionMeter.CurrentStageIndex : -1;
            if (stageIndex < 0)
            {
                ResetEventTimer();
                return;
            }

            var triggered = TriggerEvent(ChooseEventForStage(stageIndex), isManual: false);
            if (!triggered)
            {
                eventTimer = 4f;
                return;
            }

            ResetEventTimer();
        }

        private bool TriggerEvent(CorruptionEventKind eventKind, bool isManual)
        {
            if (spawnDirector == null)
            {
                return false;
            }

            var triggered = eventKind switch
            {
                CorruptionEventKind.RottingSwell => spawnDirector.TriggerCorruptionWave(SpawnDirector.CorruptionWaveClass.Fodder),
                CorruptionEventKind.SirenPressure => spawnDirector.TriggerCorruptionWave(SpawnDirector.CorruptionWaveClass.Specialist),
                CorruptionEventKind.DeepTide => spawnDirector.TriggerCorruptionWave(SpawnDirector.CorruptionWaveClass.Elite),
                CorruptionEventKind.CorruptionFront => TriggerFrontPressure(),
                _ => false
            };

            if (!triggered)
            {
                return false;
            }

            ShowBanner(eventKind, isManual);
            return true;
        }

        private bool TriggerFrontPressure()
        {
            if (!spawnDirector.TriggerCorruptionWave(SpawnDirector.CorruptionWaveClass.Specialist))
            {
                return false;
            }

            frontPressureRemaining = Mathf.Max(frontPressureRemaining, FrontPressureDuration);
            frontPressureWaveTimer = 0f;
            return true;
        }

        private void TickFrontPressure(float deltaTime)
        {
            if (frontPressureRemaining <= 0f || spawnDirector == null)
            {
                return;
            }

            if (runStateManager != null && runStateManager.BossWaveStarted)
            {
                return;
            }

            frontPressureRemaining = Mathf.Max(0f, frontPressureRemaining - deltaTime);
            frontPressureWaveTimer -= deltaTime;
            if (frontPressureWaveTimer > 0f)
            {
                return;
            }

            if (spawnDirector.TriggerCorruptionWave(SpawnDirector.CorruptionWaveClass.Fodder))
            {
                frontPressureWaveTimer = FrontPressureWaveInterval;
            }
            else
            {
                frontPressureWaveTimer = 1.5f;
            }
        }

        private void ShowBanner(CorruptionEventKind eventKind, bool isManual)
        {
            activeBannerTitle = eventKind switch
            {
                CorruptionEventKind.RottingSwell => "Rotting Swell",
                CorruptionEventKind.SirenPressure => "Siren Pressure",
                CorruptionEventKind.DeepTide => "Deep Tide",
                CorruptionEventKind.CorruptionFront => "Corruption Front",
                _ => "Corruption Surge"
            };

            if (isManual)
            {
                activeBannerTitle = $"{activeBannerTitle} [Sandbox]";
            }

            bannerTimer = 3f;
        }

        private CorruptionEventKind ChooseEventForStage(int stageIndex)
        {
            var roll = Random.value;
            return stageIndex switch
            {
                0 => CorruptionEventKind.RottingSwell,
                1 => roll < 0.65f ? CorruptionEventKind.RottingSwell : CorruptionEventKind.SirenPressure,
                2 => roll < 0.45f ? CorruptionEventKind.RottingSwell : roll < 0.80f ? CorruptionEventKind.SirenPressure : CorruptionEventKind.DeepTide,
                _ => roll < 0.35f ? CorruptionEventKind.RottingSwell : roll < 0.75f ? CorruptionEventKind.SirenPressure : roll < 0.90f ? CorruptionEventKind.DeepTide : CorruptionEventKind.CorruptionFront
            };
        }

        private void ResetEventTimer()
        {
            var stageIndex = corruptionMeter != null ? corruptionMeter.CurrentStageIndex : -1;
            if (stageIndex < 0)
            {
                eventTimer = 45f;
                return;
            }

            var range = stageIndex switch
            {
                0 => new Vector2(45f, 60f),
                1 => new Vector2(35f, 50f),
                2 => new Vector2(28f, 40f),
                _ => new Vector2(22f, 32f)
            };

            eventTimer = Random.Range(range.x, range.y);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null && bodyStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.95f, 0.90f, 0.90f, 1f);

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            bodyStyle.normal.textColor = BannerColor;
        }
    }
}
