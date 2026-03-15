using System.Collections.Generic;
using UnityEngine;

namespace Dagon.Gameplay.Spawning
{
    public enum EnemySpawnPattern
    {
        SurroundRing,
        FrontCone,
        FlankPincer,
        EliteEscort
    }

    public interface ISpawnRandom
    {
        float Range(float minInclusive, float maxInclusive);
    }

    public sealed class UnitySpawnRandom : ISpawnRandom
    {
        public float Range(float minInclusive, float maxInclusive)
        {
            return Random.Range(minInclusive, maxInclusive);
        }
    }

    public sealed class EnemySpawnPlanner
    {
        private readonly float spawnRadius;

        public EnemySpawnPlanner(float spawnRadius)
        {
            this.spawnRadius = Mathf.Max(0.1f, spawnRadius);
        }

        public List<Vector3> BuildPatternPositions(EnemySpawnPattern pattern, int count, Vector3 playerPosition, Vector3 forward, ISpawnRandom random)
        {
            var plannedPositions = new List<Vector3>(Mathf.Max(0, count));
            if (count <= 0 || random == null)
            {
                return plannedPositions;
            }

            var resolvedForward = ResolveForward(forward);
            var right = new Vector3(resolvedForward.z, 0f, -resolvedForward.x);

            switch (pattern)
            {
                case EnemySpawnPattern.SurroundRing:
                    for (var index = 0; index < count; index++)
                    {
                        var angle = (360f / Mathf.Max(1, count)) * index + random.Range(-18f, 18f);
                        var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                        plannedPositions.Add(playerPosition + direction * random.Range(spawnRadius * 0.78f, spawnRadius) + Vector3.up * 0.5f);
                    }
                    break;
                case EnemySpawnPattern.FrontCone:
                    for (var index = 0; index < count; index++)
                    {
                        var yaw = random.Range(-26f, 26f);
                        var direction = Quaternion.Euler(0f, yaw, 0f) * resolvedForward;
                        plannedPositions.Add(playerPosition + direction * random.Range(spawnRadius * 0.75f, spawnRadius * 0.96f) + Vector3.up * 0.5f);
                    }
                    break;
                case EnemySpawnPattern.FlankPincer:
                    for (var index = 0; index < count; index++)
                    {
                        var flank = index % 2 == 0 ? right : -right;
                        var mixed = (flank + resolvedForward * random.Range(-0.25f, 0.35f)).normalized;
                        plannedPositions.Add(playerPosition + mixed * random.Range(spawnRadius * 0.82f, spawnRadius) + Vector3.up * 0.5f);
                    }
                    break;
                case EnemySpawnPattern.EliteEscort:
                    plannedPositions.Add(playerPosition + resolvedForward * (spawnRadius * 0.88f) + Vector3.up * 0.5f);
                    plannedPositions.Add(playerPosition + (resolvedForward + right * 0.35f).normalized * (spawnRadius * 0.82f) + Vector3.up * 0.5f);
                    plannedPositions.Add(playerPosition + (resolvedForward - right * 0.35f).normalized * (spawnRadius * 0.82f) + Vector3.up * 0.5f);
                    plannedPositions.Add(playerPosition + (resolvedForward - right * 0.1f).normalized * (spawnRadius * 0.92f) + Vector3.up * 0.5f);
                    break;
            }

            return plannedPositions;
        }

        private static Vector3 ResolveForward(Vector3 forward)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }
    }
}
