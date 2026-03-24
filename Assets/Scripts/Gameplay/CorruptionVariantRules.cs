using UnityEngine;

namespace Dagon.Gameplay
{
    public static class CorruptionVariantRules
    {
        public readonly struct StatModifiers
        {
            public StatModifiers(float healthMultiplier, float damageMultiplier, float speedMultiplier, float cadenceMultiplier)
            {
                HealthMultiplier = healthMultiplier;
                DamageMultiplier = damageMultiplier;
                SpeedMultiplier = speedMultiplier;
                CadenceMultiplier = cadenceMultiplier;
            }

            public float HealthMultiplier { get; }
            public float DamageMultiplier { get; }
            public float SpeedMultiplier { get; }
            public float CadenceMultiplier { get; }
        }

        public static float GetEnemyCorruptionChance(float currentCorruption)
        {
            if (currentCorruption < 25f)
            {
                return 0f;
            }

            var t = Mathf.InverseLerp(25f, 100f, currentCorruption);
            return Mathf.Lerp(0.08f, 0.72f, Mathf.SmoothStep(0f, 1f, t));
        }

        public static float GetBossCorruptionChance(float currentCorruption)
        {
            if (currentCorruption < 25f)
            {
                return 0f;
            }

            var t = Mathf.InverseLerp(25f, 100f, currentCorruption);
            return Mathf.Lerp(0.04f, 0.38f, Mathf.SmoothStep(0f, 1f, t));
        }

        public static float GetEnemyHealthMultiplierFromCorruption(float currentCorruption)
        {
            if (currentCorruption <= 0f)
            {
                return 1f;
            }

            return 1f + (currentCorruption / 200f);
        }

        public static StatModifiers GetEnemyModifiers(EnemyArchetype archetype)
        {
            return archetype switch
            {
                EnemyArchetype.Fodder => new StatModifiers(1.6f, 1.25f, 1.12f, 1.08f),
                EnemyArchetype.Specialist => new StatModifiers(1.75f, 1.35f, 1.10f, 1.14f),
                EnemyArchetype.Elite => new StatModifiers(2f, 1.4f, 1.08f, 1.12f),
                _ => new StatModifiers(1f, 1f, 1f, 1f)
            };
        }

        public static StatModifiers GetBossModifiers()
        {
            return new StatModifiers(1.8f, 1.3f, 1.08f, 1.12f);
        }

        public enum EnemyArchetype
        {
            Fodder,
            Specialist,
            Elite
        }
    }
}
